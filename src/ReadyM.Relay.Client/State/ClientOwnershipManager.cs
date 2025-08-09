using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Relay.Client.State;

public class ClientOwnershipManager(ClientState state, NetworkedOwnershipManager ownership)
{
    public bool TryGetOwner(NetworkId netId, out PlayerId ownerId)
        => ownership.TryGetOwner(netId, out ownerId);
    
    public bool TryGetOwner(Entity entity, out PlayerId ownerId)
        => ownership.TryGetOwner(entity, out ownerId);
    
    public bool OwnsEntity(NetworkId netId)
        => ownership.TryGetOwner(netId, out var ownerId) && ownerId == state.LocalPlayerId;
    
    public bool OwnsEntity(Entity entity)
        => ownership.TryGetOwner(entity, out var ownerId) && ownerId == state.LocalPlayerId;
}