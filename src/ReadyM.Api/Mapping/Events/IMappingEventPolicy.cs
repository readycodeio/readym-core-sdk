namespace ReadyM.Api.Mapping.Events;

public interface IMappingEventPolicy<TContext> : IMappingEventPolicyBase
    where TContext : struct
{
    bool ShouldEventPropagateToEcs(in TContext context);
    bool ShouldEventPropagateToGame(in TContext context);
    bool ShouldGameEventRunLocally(in TContext context, out EventSource eventSource);
}