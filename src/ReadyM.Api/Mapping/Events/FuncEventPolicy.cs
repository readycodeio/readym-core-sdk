using ReadyM.Api.Helpers;

namespace ReadyM.Api.Mapping.Events;

public class FuncEventPolicy<TEvent, TContext>(
    ShouldPropagateToEcsDelegate<TContext> shouldPropagateToEcs,
    ShouldPropagateToGameDelegate<TContext> shouldPropagateToGame,
    ShouldRunLocallyDelegate<TContext> shouldRunLocally,
    DataSideChannel sideChannel)
    : MappingEventPolicyBase<TEvent, TContext>(sideChannel)
    where TContext : struct
{
    protected override bool ShouldEventPropagateToEcsImpl(in TContext context)
    {
        return shouldPropagateToEcs(in context);
    }
    protected override bool ShouldEventPropagateToGameImpl(in TContext context)
    {
        return shouldPropagateToGame(in context);
    }
    protected override bool ShouldGameEventRunLocallyImpl(in TContext context)
    {
        return shouldRunLocally(in context);
    }
}