using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Relay.Client.State;

internal class ClientOwnershipManager(ClientState state, NetworkedOwnershipManager ownership)
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

    public bool OwnsEntity(MetadataComponent meta)
        => ownership.TryGetOwner(meta, out var ownerId) && ownerId == state.LocalPlayerId;
}