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
    public PlayerId LocalPlayer => localPlayer;

    public override bool Sees(PlayerId player, Entity scope)
        => false;

    /// <summary>Whether the local player sees the scope.</summary>
    public bool Sees(Entity scope)
        => Sees(localPlayer, scope);

    /// <summary>Creates an entity owned by the local player in a scope it sees, owed to the server.</summary>
    public Entity Create(ArchetypeId archetype, Entity scope)
        => default;

    /// <summary>Deletes an entity the local player owns, owed to the server.</summary>
    public void Delete(Entity entity)
    {
    }

    /// <summary>
    /// The server says the local player now sees this scope, with the scope's own entity unless it is the global scope;
    /// the scope's entities follow in the same message. Returns the scope entity, a null entity for the global scope.
    /// </summary>
    public Entity ApplyEnterScope(NetworkId scope, MetadataComponent? scopeEntity)
        => default;

    /// <summary>The server says the local player no longer sees this scope: its entities are deleted here, owing nothing.</summary>
    public void ApplyLeaveScope(NetworkId scopeNetId)
    {
    }

    /// <summary>An entity the server sent; one the client already holds takes the new scope and owner.</summary>
    public Entity ApplyCreate(NetworkId scopeNetId, MetadataComponent meta)
        => default;

    public void ApplyDelete(NetworkId netId)
    {
    }

    public void ApplyOwnerChange(NetworkId netId, PlayerId owner)
    {
    }

    public void ApplyScopeChange(NetworkId netId, NetworkId scopeNetId)
    {
    }
}
