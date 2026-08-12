using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping.Policies.Event.Common;

/// Used for events that are only sent to clients from the server and therefore are never triggered locally.
internal class AlwaysPropagatesToGameOnlyEventPolicy<TEvent>(DataSideChannel sideChannel) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool CanGameEventNotifyEcsImpl(in EmptyContext context)
    {
        return false;
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