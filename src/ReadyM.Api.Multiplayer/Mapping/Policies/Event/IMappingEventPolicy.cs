namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event;

public interface IMappingEventPolicy<TContext> : IMappingEventPolicyBase
{
    bool CanGameEventNotifyEcs(in TContext context);

    bool CanEcsInvokeGameEvent(in TContext context);

    bool CanGameEventRunLocally(in TContext context);
}