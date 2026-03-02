using System;

namespace ReadyM.Api.Mapping.Events;

public interface IMappingEventPolicy<TContext> : IMappingEventPolicyBase
    where TContext : struct
{
    bool ShouldEventPropagateToEcs(in TContext context);

    [Obsolete("Is this event needed in the API?")]
    bool ShouldEventPropagateToGame(in TContext context);

    bool ShouldGameEventRunLocally(in TContext context, out EventSource eventSource);
}