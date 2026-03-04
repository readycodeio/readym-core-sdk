using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;
using ReadyM.Relay.Client.State;

namespace ReadyM.Relay.Client.Mapping.Policies;

public class OwnershipEventPolicy<TEvent>(
    ClientOwnershipManager ownership,
    DataSideChannel sideChannel
) : MappingEventPolicyBase<TEvent, Entity>(sideChannel)
{
    protected override bool CanGameEventNotifyEcsImpl(in Entity context)
    {
        return ownership.OwnsEntity(context);
    }

    protected override bool CanEcsInvokeGameEventImpl(in Entity context)
    {
        return !ownership.OwnsEntity(context);
    }

    protected override bool CanGameEventRunLocallyImpl(in Entity context)
    {
        return ownership.OwnsEntity(context);
    }
}