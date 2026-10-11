using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Accounting;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Relay.Client.Accounting;

/// <summary>
/// The client's ledger: the local player's own creates and deletes, owed to the server, and the scopes the server says
/// it sees. ECS thread only.
/// </summary>
internal sealed class ClientLedger(Store world, NetworkedEntityManager netEntity, PlayerId localPlayer, ILogger logger)
    : EntityLedgerBase(world, netEntity, logger)
{
    // The scopes the server says the local player sees, with their net ids; a null entity is the global scope.
    private readonly Dictionary<Entity, NetworkId> _seen = [];

    private readonly List<NetworkId> _deletes = [];
    private bool _deletesSent;

    // Scopes whose unsent creates were sent to the server this round.
    private readonly HashSet<Entity> _createsSent = [];
    private readonly List<Entity> _createScopes = [];

    // Above zero while a message from the server is applied: what it deletes is owed to nobody.
    private int _applying;

    // Requests to change area or cells that the server has not confirmed yet, oldest first; a cell change lists the
    // cells it leaves.
    private readonly Queue<List<Entity>?> _unconfirmed = new();
    private int _areaChanges;
    private List<Entity>? _cellChange;

    public PlayerId LocalPlayer => localPlayer;

    public override bool Sees(PlayerId player, Entity scope)
        => player == localPlayer && _seen.ContainsKey(scope);

    /// <summary>Whether the local player sees the scope.</summary>
    public bool Sees(Entity scope)
        => Sees(localPlayer, scope);

    /// <summary>
    /// Whether the local player may create or delete in the scope: it sees it; outside the global scope, no change of
    /// area is unconfirmed, since until then a scope it holds may be one it is leaving or left and rejoined; it is not
    /// a cell the unconfirmed change of cells leaves; and it is not the player scope of another player, a read-only view.
    /// </summary>
    public bool MayChange(Entity scope)
    {
        if (!Sees(scope))
            return false;
        if (scope == default)
            return true;

        return _areaChanges == 0
               && (_cellChange is null || !_cellChange.Contains(scope))
               && (!scope.HasComponent<PlayerScopeComponent>() || scope.GetComponent<MetadataComponent>().Owner == localPlayer);
    }

    /// <summary>
    /// Whether the local player may ask to change its cells: it is properly in an area, every change of area confirmed
    /// and the area sent, and no change of cells is unconfirmed.
    /// </summary>
    public bool MayChangeCells
    {
        get
        {
            if (_areaChanges > 0 || _cellChange is not null)
                return false;

            foreach (var scope in _seen.Keys)
            {
                if (scope != default && scope.HasComponent<AreaScopeComponent>())
                    return true;
            }

            return false;
        }
    }

    /// <summary>The local player asked the server to join an area or leave its area; allowed with a change of cells unconfirmed.</summary>
    public void AreaChangeRequested()
    {
        _areaChanges++;
        _unconfirmed.Enqueue(null);
    }

    /// <summary>The local player asked the server to change its cells, leaving these; throws unless <see cref="MayChangeCells"/>.</summary>
    public void CellChangeRequested(IEnumerable<Entity> cellsLeft)
    {
        if (!MayChangeCells)
            throw new InvalidOperationException("The local player may change its cells only once properly in an area, one change at a time");

        _cellChange = [..cellsLeft];
        _unconfirmed.Enqueue(_cellChange);
    }

    /// <summary>The server confirmed the oldest change of area or cells the local player asked for.</summary>
    public void MembershipConfirmed()
    {
        if (_unconfirmed.Count == 0)
            throw new InvalidOperationException("The server confirmed a change of area or cells nobody asked for");

        if (_unconfirmed.Dequeue() is null)
            _areaChanges--;
        else
            _cellChange = null;
    }

    /// <summary>Creates an entity owned by the local player in a scope it may change, owed to the server.</summary>
    public Entity Create(ArchetypeId archetype, Entity scope)
    {
        if (!MayChange(scope))
            throw new InvalidOperationException($"The local player cannot create in {scope} now: see MayChange");

        Changing();
        var (entity, _) = NetEntity.CreateNetworkedEntity(archetype, scope == default ? null : scope);
        MarkUnsent(entity, PlayerId.Server);
        return entity;
    }

    /// <summary>Deletes a client-deletable entity the local player owns, owed to the server.</summary>
    public void Delete(Entity entity)
    {
        var meta = entity.GetComponent<MetadataComponent>();
        if (meta.Owner != localPlayer || !entity.Tags.Has<ClientDeletableTag>() || !MayChange(ScopeOf(entity)))
            throw new InvalidOperationException($"The local player cannot delete {meta.NetId}: only its own client-deletable entities, where MayChange allows");

        Changing();
        entity.DeleteEntity();
    }

    /// <summary>
    /// The server says the local player now sees this scope, with the scope's own entity unless it is the global scope;
    /// the scope's entities follow in the same message. Returns the scope entity, a null entity for the global scope.
    /// </summary>
    public Entity ApplyEnterScope(NetworkId scope, MetadataComponent? scopeEntity)
    {
        Changing();
        _applying++;
        try
        {
            if (scope == default)
            {
                _seen[default] = default;
                return default;
            }

            Entity entity;
            if (NetEntity.TryGetEntityByNetworkId(scope, out var held))
            {
                entity = held.Value;
                if (scopeEntity is { } meta)
                    entity.GetComponent<MetadataComponent>().Owner = meta.Owner;
            }
            else
            {
                if (scopeEntity is not { } meta)
                    throw new InvalidOperationException($"The server let the local player into scope {scope} without its entity");
                entity = NetEntity.CreateRemoteNetworkedEntity(meta, null);
            }

            _seen[entity] = scope;
            return entity;
        }
        finally
        {
            _applying--;
        }
    }

    /// <summary>The server says the local player no longer sees this scope: its entities are deleted here, owing nothing.</summary>
    public void ApplyLeaveScope(NetworkId scopeNetId)
    {
        Changing();
        _applying++;
        try
        {
            if (!NetEntity.TryGetEntityByNetworkId(scopeNetId, out var held))
            {
                Forget(scopeNetId);
                return;
            }

            var scope = held.Value;
            _seen.Remove(scope);
            DeleteContents(scope);
            if (scope.Tags.Has<ScopeEntityTag>())
                scope.DeleteEntity();
        }
        finally
        {
            _applying--;
        }
    }

    /// <summary>An entity the server sent; one the client already holds takes the new scope and owner.</summary>
    public Entity ApplyCreate(NetworkId scopeNetId, MetadataComponent meta)
    {
        Changing();
        _applying++;
        try
        {
            var scope = ScopeByNetId(scopeNetId);
            if (NetEntity.TryGetEntityByNetworkId(meta.NetId, out var held))
            {
                var entity = held.Value;
                entity.GetComponent<MetadataComponent>().Owner = meta.Owner;
                PutInScope(entity, scope);
                return entity;
            }

            return NetEntity.CreateRemoteNetworkedEntity(meta, scope == default ? null : scope);
        }
        finally
        {
            _applying--;
        }
    }

    /// <summary>
    /// The server deleted an entity; one the client deleted itself is gone already. Deleting a player entity also ends
    /// seeing its player scope, with everything in it.
    /// </summary>
    public void ApplyDelete(NetworkId netId)
    {
        Changing();
        _applying++;
        try
        {
            if (!NetEntity.TryGetEntityByNetworkId(netId, out var held))
                return;

            var entity = held.Value;
            if (_seen.Remove(entity))
                DeleteContents(entity);
            entity.DeleteEntity();
        }
        finally
        {
            _applying--;
        }
    }

    public void ApplyOwnerChange(NetworkId netId, PlayerId owner)
    {
        Changing();
        if (NetEntity.TryGetEntityByNetworkId(netId, out var held))
            held.Value.GetComponent<MetadataComponent>().Owner = owner;
    }

    /// <summary>
    /// An entity the server moved, read against what the client holds and sees now: a move if it holds the entity and
    /// sees the new scope, a create if it sees the new scope only, a delete if it holds the entity only, else nothing.
    /// </summary>
    public void ApplyScopeChange(NetworkId scopeNetId, MetadataComponent meta)
    {
        Changing();
        _applying++;
        try
        {
            var held = NetEntity.TryGetEntityByNetworkId(meta.NetId, out var found);
            var sees = TryGetScope(scopeNetId, out var scope) && _seen.ContainsKey(scope);
            if (held && sees)
            {
                found!.Value.GetComponent<MetadataComponent>().Owner = meta.Owner;
                PutInScope(found.Value, scope);
            }
            else if (sees)
            {
                NetEntity.CreateRemoteNetworkedEntity(meta, scope == default ? null : scope);
            }
            else if (held)
            {
                found!.Value.DeleteEntity();
            }
        }
        finally
        {
            _applying--;
        }
    }

    /// <summary>One create entry per scope with unsent creates, and one delete entry, each owed to the server.</summary>
    protected override void Collect()
    {
        _createScopes.Clear();
        foreach (var entity in MarkedEntities())
        {
            var scope = ScopeOf(entity);
            if (!_createsSent.Contains(scope) && !_createScopes.Contains(scope))
                _createScopes.Add(scope);
        }

        foreach (var scope in _createScopes)
        {
            _seen.TryGetValue(scope, out var scopeNetId);
            Owe(LedgerEntryKind.Create, (scope, scope, true, PlayerId.Server, default), scopeNetId, PlayerId.Server, [PlayerId.Server], null);
        }

        if (_deletes.Count > 0 && !_deletesSent)
            Owe(LedgerEntryKind.Delete, (default, default, false, PlayerId.Server, default), default, PlayerId.Server, [PlayerId.Server], [.._deletes]);
    }

    protected override void OnSent(in LedgerEntry entry, int source)
    {
        if (entry.Kind == LedgerEntryKind.Delete)
            _deletesSent = true;
        else
            _createsSent.Add(entry.Scope);
    }

    protected override void OnDeleted(Entity entity)
    {
        if (_applying > 0 || entity.Tags.Has<LocallyCreatedEntityTag>())
            return;

        var meta = entity.GetComponent<MetadataComponent>();
        if (meta.Owner == localPlayer)
            _deletes.Add(meta.NetId);
    }

    protected override void OnCommit()
    {
        _deletes.Clear();
        _deletesSent = false;
        _createsSent.Clear();
    }

    private Entity ScopeByNetId(NetworkId scopeNetId)
        => TryGetScope(scopeNetId, out var scope)
            ? scope
            : throw new InvalidOperationException($"The server named scope {scopeNetId}, which the client does not hold");

    /// <summary>The scope entity a net id names, a null entity for the global scope; false if the client holds none.</summary>
    private bool TryGetScope(NetworkId scopeNetId, out Entity scope)
    {
        scope = default;
        if (scopeNetId == default)
            return true;
        if (!NetEntity.TryGetEntityByNetworkId(scopeNetId, out var found))
            return false;

        scope = found.Value;
        return true;
    }

    /// <summary>Stops seeing a scope whose entity is gone already.</summary>
    private void Forget(NetworkId scopeNetId)
    {
        foreach (var pair in _seen)
        {
            if (pair.Value == scopeNetId)
            {
                _seen.Remove(pair.Key);
                return;
            }
        }
    }
}
