using System;

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

    void PropagateToEcs<TEvent>(in TEvent ev)
        where TEvent : struct;
    void PropagateToGame<TEvent>(in TEvent ev)
        where TEvent : struct;
    void TriggerEvent<TEvent>(in TEvent ev)
        where TEvent : struct;
}