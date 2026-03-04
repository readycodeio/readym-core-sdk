using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Relay.Client.State;

namespace ReadyM.Relay.Client.Mapping.Policies;

public class OwnershipDataPolicy(ClientOwnershipManager ownership) : IMappingDataPolicy<Entity>
{
    public Type ContextType
        => typeof(Entity);
    
    public bool ShouldEcsCopyToGame(in Entity context)
        => !ownership.OwnsEntity(context);

    public bool ShouldGameCopyToEcs(in Entity context)
        => ownership.OwnsEntity(context);

    public bool ShouldGameSetLocally(in Entity context)
        => ownership.OwnsEntity(context);
}