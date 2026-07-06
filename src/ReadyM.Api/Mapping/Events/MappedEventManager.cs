using System;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping.Events;

internal class MappedEventManager(
    DataSideChannel sideChannel,
    IMappingPolicyDirectory policyDir,
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
            incomingEcsEventQueue.Invoke(ev);
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
            incomingGameEventQueue.Invoke(ev);
        }

        return true;
    }
}