using System;

namespace ReadyM.Api.Mapping.Events;

public class FuncEventPolicy<TContext>(
    ShouldPropagateToEcsDelegate<TContext> shouldPropagateToEcs,
    ShouldPropagateToGameDelegate<TContext> shouldPropagateToGame,
    ShouldRunLocallyDelegate<TContext> shouldRunLocally)
    : IMappingEventPolicy<TContext>
    where TContext : struct
{
    public Type ContextType
        => typeof(TContext);
    
    public bool ShouldEventPropagateToEcs(in TContext context)
        => shouldPropagateToEcs(in context);

    public bool ShouldEventPropagateToGame(in TContext context)
        => shouldPropagateToGame(in context);

    public bool ShouldGameEventRunLocally(in TContext context, out EventSource eventSource)
        => shouldRunLocally(in context, out eventSource);
}