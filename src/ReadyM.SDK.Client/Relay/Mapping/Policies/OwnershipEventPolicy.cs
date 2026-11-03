using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.SDK.Client.Relay.State;

namespace ReadyM.SDK.Client.Relay.Mapping.Policies;

internal class OwnershipEventPolicy<TEvent>(
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