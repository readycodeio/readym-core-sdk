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

namespace ReadyM.SDK.Client.Relay.Accounting;

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
    private int _deleteEntry;
    private bool _deletesSent;

    // Above zero while a message from the server is applied: what it deletes is owed to nobody.
    private int _applying;

    // Requests to change area or cells that the server has not confirmed yet.
    private int _unconfirmed;

    public PlayerId LocalPlayer => localPlayer;

    public override bool Sees(PlayerId player, Entity scope)
        => player == localPlayer && _seen.ContainsKey(scope);

    /// <summary>Whether the local player sees the scope.</summary>
    public bool Sees(Entity scope)
        => Sees(localPlayer, scope);

    /// <summary>
    /// Whether the local player may create or delete in the scope: it sees it; outside the global scope, the server has
    /// confirmed every change of area or cells it asked for, since until then a scope it holds may be one it is leaving;
    /// and it is not another player's player scope, which that player's moves take the local player in and out of.
    /// </summary>
    public bool MayChange(Entity scope)
        => Sees(scope)
           && (scope == default
               || (_unconfirmed == 0
                   && (!scope.HasComponent<PlayerScopeComponent>() || scope.GetComponent<MetadataComponent>().Owner == localPlayer)));

    /// <summary>The local player asked the server to change its area or cells.</summary>
    public void MembershipRequested()
        => _unconfirmed++;

    /// <summary>The server confirmed the oldest change of area or cells the local player asked for.</summary>
    public void MembershipConfirmed()
    {
        if (_unconfirmed == 0)
            throw new InvalidOperationException("The server confirmed a change of area or cells nobody asked for");
        _unconfirmed--;
    }

    /// <summary>Creates an entity owned by the local player in a scope it may change, owed to the server.</summary>
    public Entity Create(ArchetypeId archetype, Entity scope)
    {
        if (!MayChange(scope))
            throw new InvalidOperationException($"The local player cannot create in {scope} now: see MayChange");

        Changing();
        var (entity, _) = NetEntity.CreateNetworkedEntity(archetype, scope == default ? null : scope);
        entity.RemoveTag<LocallyCreatedEntityTag>();
        SetGroup(entity, new LedgerGroup(scope, scope, true, localPlayer, default));
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

    protected override void Collect()
    {
        foreach (var group in Groups)
        {
            if (group.Main.Contains(PlayerId.Server) || EntitiesOf(group.Id).Count == 0)
                continue;

            var scope = group.Key.Scope;
            _seen.TryGetValue(scope, out var scopeNetId);
            Owe(LedgerEntryKind.Create, scope, scopeNetId, default, PlayerId.Server, [PlayerId.Server], null, group.Id);
        }

        if (_deletes.Count > 0 && !_deletesSent)
            Owe(LedgerEntryKind.Delete, default, default, default, PlayerId.Server, [PlayerId.Server], [.._deletes], _deleteEntry);
    }

    protected override void OnSent(in LedgerEntry entry)
    {
        if (entry.Kind == LedgerEntryKind.Delete)
            _deletesSent = true;
        else
            GroupOf(entry.Group).Main.Add(PlayerId.Server);
    }

    protected override void OnDeleted(Entity entity)
    {
        if (_applying > 0 || TryGetGroup(entity, out _))
            return;

        var meta = entity.GetComponent<MetadataComponent>();
        if (meta.Owner != localPlayer)
            return;

        if (_deleteEntry == 0)
            _deleteEntry = NextId();
        _deletes.Add(meta.NetId);
    }

    protected override void OnCommit()
    {
        _deletes.Clear();
        _deletesSent = false;
        _deleteEntry = 0;
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
