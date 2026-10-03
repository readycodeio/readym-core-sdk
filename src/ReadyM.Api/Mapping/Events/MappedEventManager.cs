using System;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;

namespace ReadyM.Api.Mapping.Events;

internal class MappedEventManager(
    DataSideChannel sideChannel,
    GameEventContextRegistry contexts,
    ILogger logger
) : IMappedEventManager
{
    protected readonly EventQueue incomingEcsEventQueue = new(logger);
    protected readonly EventQueue incomingGameEventQueue = new(logger);

    public void RegisterEcsEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct
        => incomingEcsEventQueue.RegisterHandler(handler);

    public void RegisterEcsEventHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
        where TEvent : struct
        => incomingEcsEventQueue.RegisterHandler(handler, arg);

    public void RegisterEcsEventHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        where TEvent : struct
        => incomingEcsEventQueue.RegisterHandler(handler, arg0, arg1);

    public void RegisterEcsEventHandler<TEvent, TArg0, TArg1, TArg2>(Action<TEvent, TArg0, TArg1, TArg2> handler, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        where TEvent : struct
        => incomingEcsEventQueue.RegisterHandler(handler, arg0, arg1, arg2);

    public void RegisterGameEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct
        => incomingGameEventQueue.RegisterHandler(handler);

    public void RegisterGameEventHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
        where TEvent : struct
        => incomingGameEventQueue.RegisterHandler(handler, arg);

    public void RegisterGameEventHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        where TEvent : struct
        => incomingGameEventQueue.RegisterHandler(handler, arg0, arg1);

    public void InvokeInGameAndNotifyEcs<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent
    {
        NotifyEcsIfApplicable(ev);
        InvokeInGameIfApplicable(ev);
    }

    /// <inheritdoc/>
    public bool NotifyEcsIfApplicable<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent
    {
        if (CanGameEventNotifyEcs(ev) != GameEventNotifyResult.Notify)
            return false;

        using (sideChannel.PushScope<PropagatingToEcsScope<TEvent>>())
        {
            incomingEcsEventQueue.Invoke(ev);
        }

        return true;
    }

    /// <inheritdoc/>
    public bool InvokeInGameIfApplicable<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent
    {
        if (!CanEcsInvokeGameEvent(ev).Runs())
            return false;

        using (sideChannel.PushScope<PropagatingToGameScope<TEvent>>())
        {
            incomingGameEventQueue.Invoke(ev);
        }

        return true;
    }

    public GameEventNotifyResult CanGameEventNotifyEcs<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent
        => sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? GameEventNotifyResult.DontNotify
            : ev.CanGameEventNotifyEcs(contexts);

    public GameEventResult CanGameEventRunLocally<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent
        => sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? GameEventResult.RunAll
            : ev.CanGameEventRunLocally(contexts);

    public GameEventResult CanEcsInvokeGameEvent<TEvent>(in TEvent ev)
        where TEvent : struct, IGameEvent
        => sideChannel.HasData<PropagatingToEcsScope<TEvent>>()
            ? GameEventResult.DontRun
            : ev.CanEcsInvokeGameEvent(contexts);
}
