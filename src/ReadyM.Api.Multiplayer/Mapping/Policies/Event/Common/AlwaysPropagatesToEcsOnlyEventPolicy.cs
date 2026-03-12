using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event.Common;

/// Used with events that are only happening on the game-side and it makes no sense to trigger them manually.
internal class AlwaysPropagatesToEcsOnlyEventPolicy<TEvent>(DataSideChannel sideChannel) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool CanGameEventNotifyEcsImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool CanEcsInvokeGameEventImpl(in EmptyContext context)
    {
        return false;
    }

    protected override bool CanGameEventRunLocallyImpl(in EmptyContext context)
    {
        return true;
    }
}