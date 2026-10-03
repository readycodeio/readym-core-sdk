using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Relay.Client.State;

internal class ClientOwnershipManager(ClientState state, NetworkedOwnershipManager ownership, Store world)
{
    public bool TryGetOwner(NetworkId netId, out PlayerId ownerId)
        => ownership.TryGetOwner(netId, out ownerId);

    public bool TryGetOwner(Entity entity, out PlayerId ownerId)
        => ownership.TryGetOwner(entity, out ownerId);

    public bool TryGetOwner(MetadataComponent meta, out PlayerId ownerId)
        => ownership.TryGetOwner(meta, out ownerId);

    public bool OwnsEntity(NetworkId netId)
        => ownership.TryGetOwner(netId, out var ownerId) && ownerId == state.LocalPlayerId;

    public bool OwnsEntity(Entity entity)
        => ownership.TryGetOwner(entity, out var ownerId) && ownerId == state.LocalPlayerId;

    /// <summary>The null entity, or one no longer in the world, is owned by no one.</summary>
    public bool OwnsEntity(RawEntity entity)
    {
        var resolved = world.GetEntityByRawEntity(entity);
        return !resolved.IsNull && OwnsEntity(resolved);
    }

    public bool OwnsEntity(MetadataComponent meta)
        => ownership.TryGetOwner(meta, out var ownerId) && ownerId == state.LocalPlayerId;
}