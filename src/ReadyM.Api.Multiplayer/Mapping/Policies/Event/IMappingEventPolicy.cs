using ReadyM.Api.Multiplayer.Mapping.Events;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event;

public interface IMappingEventPolicy<TContext> : IMappingEventPolicyBase
    where TContext : struct
{
    bool CanGameEventNotifyEcs(in TContext context);

    bool CanEcsInvokeGameEvent(in TContext context);

    bool CanGameEventRunLocally(in TContext context, out EventSource eventSource);
}