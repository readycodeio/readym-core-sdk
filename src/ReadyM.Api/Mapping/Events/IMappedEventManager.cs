using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Events;

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

    /// Invoke the registered handlers for the event on the ECS side.
    void NotifyEcs<TEvent>(in TEvent ev)
        where TEvent : struct;

    /// Invoke the registered handlers for the event on the game side.
    void InvokeInGame<TEvent>(in TEvent ev)
        where TEvent : struct;

    /// Propagate the event to both the ECS and the game, regardless of the policy.
    void InvokeInGameAndNotifyEcs<TEvent>(in TEvent ev)
        where TEvent : struct;

    /// Propagate the event to the ECS if the event policy allows it.
    void NotifyEcsIfApplicable<TEvent>(in TEvent ev, Entity context) where TEvent : struct, IMappingContext<Entity>;
    void InvokeInGameIfApplicable<TEvent>(in TEvent ev, Entity context) where TEvent : struct, IMappingContext<Entity>;

    /// Is the game allowed to run the event logic locally right now?
    /// Returns <c>true</c> if invoked in ECS scope, otherswise consults the policy.
    bool CanGameEventRunLocally<TEvent>(Entity context)
        where TEvent : struct, IMappingContext<Entity>;
}