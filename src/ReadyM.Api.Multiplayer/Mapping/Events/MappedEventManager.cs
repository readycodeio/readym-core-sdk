using System;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace ReadyM.Api.Multiplayer.Mapping.Events;

internal class MappedEventManager(DataSideChannel sideChannel, IMappingPolicyDirectory policyDir, ILogger logger) : IMappedEventManager
{
    private readonly EventQueue _incomingEcsEventQueue = new(logger);
    private readonly EventQueue _incomingGameEventQueue = new(logger);

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

    public void InvokeInGameAndNotifyEcs<TEvent, TContext>(in TEvent ev, TContext context)
        where TEvent : struct, IMappingContext<TContext>
    {
        NotifyEcsIfApplicable(ev, context);
        InvokeInGameIfApplicable(ev, context);
    }

    /// <inheritdoc/>
    public bool NotifyEcsIfApplicable<TEvent, TContext>(in TEvent ev, TContext context)
        where TEvent : struct, IMappingContext<TContext>
    {
        if (!policyDir.ForEvent<TEvent, TContext>().CanGameEventNotifyEcs(context))
            return false;

        using (sideChannel.PushScope<PropagatingToEcsScope<TEvent>>())
        {
            _incomingEcsEventQueue.Invoke(ev);
        }

        return true;
    }

    /// <inheritdoc/>
    public bool InvokeInGameIfApplicable<TEvent, TContext>(in TEvent ev, TContext context)
        where TEvent : struct, IMappingContext<TContext>
    {
        if (!policyDir.ForEvent<TEvent, TContext>().CanEcsInvokeGameEvent(context))
            return false;

        using (sideChannel.PushScope<PropagatingToGameScope<TEvent>>())
        {
            _incomingGameEventQueue.Invoke(ev);
        }

        return true;
    }
}