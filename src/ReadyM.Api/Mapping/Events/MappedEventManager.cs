using System;
using ReadyM.Api.Helpers;

namespace ReadyM.Api.Mapping.Events;

public class MappedEventManager(DataSideChannel sideChannel) : IMappedEventManager
{
    private readonly EventQueue _incomingEcsEventQueue = new();
    private readonly EventQueue _incomingGameEventQueue = new();

    public void RegisterEcsEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct
        => _incomingEcsEventQueue.RegisterHandler(handler);

    public void RegisterEcsEventHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
        where TEvent : struct
        => _incomingEcsEventQueue.RegisterHandler(handler, arg);

    public void RegisterEcsEventHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        where TEvent : struct
        => _incomingEcsEventQueue.RegisterHandler(handler, arg0, arg1);
    
    public void RegisterGameEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct
        => _incomingGameEventQueue.RegisterHandler(handler);
    
    public void RegisterGameEventHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
        where TEvent : struct
        => _incomingGameEventQueue.RegisterHandler(handler, arg);
    
    public void RegisterGameEventHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        where TEvent : struct
        => _incomingGameEventQueue.RegisterHandler(handler, arg0, arg1);
    
    public void PropagateToEcs<TEvent>(in TEvent ev)
        where TEvent : struct
    {
        using (sideChannel.PushScope<PropagatingToEcsScope<TEvent>>())
        {
            _incomingEcsEventQueue.Invoke(ev);
        }
    }

    public void PropagateToGame<TEvent>(in TEvent ev)
        where TEvent : struct
    {
        using (sideChannel.PushScope<PropagatingToGameScope<TEvent>>())
        {
            _incomingGameEventQueue.Invoke(ev);
        }
    }

    public void TriggerEvent<TEvent>(in TEvent ev) where TEvent : struct
    {
        using (sideChannel.PushScope<PropagatingToEcsScope<TEvent>>())
        using (sideChannel.PushScope<PropagatingToGameScope<TEvent>>())
        {
            _incomingEcsEventQueue.Invoke(ev);
            _incomingGameEventQueue.Invoke(ev);
        }
    }
}