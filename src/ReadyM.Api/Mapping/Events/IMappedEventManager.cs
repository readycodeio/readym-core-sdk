using System;

namespace ReadyM.Api.Mapping.Events;

internal interface IMappedEventManager
{
    void RegisterEcsEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct;

    void RegisterEcsEventHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
        where TEvent : struct;

    void RegisterEcsEventHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        where TEvent : struct;

    void RegisterEcsEventHandler<TEvent, TArg0, TArg1, TArg2>(Action<TEvent, TArg0, TArg1, TArg2> handler, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        where TEvent : struct;

    void RegisterGameEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct;

    void RegisterGameEventHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
        where TEvent : struct;

    void RegisterGameEventHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        where TEvent : struct;

    /// Notify the ECS, then invoke in game, each as the event's policy allows.
    void InvokeInGameAndNotifyEcs<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent;

    /// <returns>Whether the event was propagated.</returns>
    bool NotifyEcsIfApplicable<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent;

    /// <returns>Whether the event was propagated.</returns>
    bool InvokeInGameIfApplicable<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent;

    /// The event policy's answers, for call sites that need one without notifying or invoking anything.
    GameEventNotifyResult CanGameEventNotifyEcs<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent;

    GameEventResult CanGameEventRunLocally<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent;

    GameEventResult CanEcsInvokeGameEvent<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent;
}
