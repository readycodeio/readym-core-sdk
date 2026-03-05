using System;

namespace ReadyM.Api.Multiplayer.Mapping.Events;

public interface IMappedEventManager
{
    void RegisterEcsEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct;

    void RegisterEcsEventHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
        where TEvent : struct;

    void RegisterEcsEventHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        where TEvent : struct;

    void RegisterGameEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct;

    void RegisterGameEventHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
        where TEvent : struct;

    void RegisterGameEventHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        where TEvent : struct;

    /// Propagate the event to both the ECS and the game, regardless of the policy.
    void InvokeInGameAndNotifyEcs<TEvent>(in TEvent ev)
        where TEvent : struct;

    /// Propagate the event to the ECS if the event policy allows it.
    /// <returns>Whether the event was propagated.</returns>
    bool NotifyEcsIfApplicable<TEvent, TContext>(in TEvent ev, TContext context)
        where TEvent : struct, IMappingContext<TContext>
        where TContext : struct;

    /// Propagate the event to the game if the event policy allows it.
    /// <returns>Whether the event was propagated.</returns>
    bool InvokeInGameIfApplicable<TEvent, TContext>(in TEvent ev, TContext context)
        where TEvent : struct, IMappingContext<TContext>
        where TContext : struct;
}