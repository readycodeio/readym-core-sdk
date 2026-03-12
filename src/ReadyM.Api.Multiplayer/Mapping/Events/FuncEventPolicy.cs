using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;

namespace ReadyM.Api.Multiplayer.Mapping.Events;

internal class FuncEventPolicy<TEvent, TContext>(
    ShouldPropagateToEcsDelegate<TContext> shouldPropagateToEcs,
    ShouldPropagateToGameDelegate<TContext> shouldPropagateToGame,
    ShouldRunLocallyDelegate<TContext> shouldRunLocally,
    DataSideChannel sideChannel)
    : MappingEventPolicyBase<TEvent, TContext>(sideChannel)
    where TContext : struct
{
    protected override bool CanGameEventNotifyEcsImpl(in TContext context)
    {
        return shouldPropagateToEcs(in context);
    }
    protected override bool CanEcsInvokeGameEventImpl(in TContext context)
    {
        return shouldPropagateToGame(in context);
    }
    protected override bool CanGameEventRunLocallyImpl(in TContext context)
    {
        return shouldRunLocally(in context);
    }
}