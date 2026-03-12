using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event.Common;

internal class AlwaysPropagatesEventPolicy<TEvent>(DataSideChannel sideChannel) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool CanGameEventNotifyEcsImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool CanEcsInvokeGameEventImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool CanGameEventRunLocallyImpl(in EmptyContext context)
    {
        return true;
    }
}