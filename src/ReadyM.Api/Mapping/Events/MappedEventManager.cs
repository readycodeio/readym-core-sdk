using System;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping.Events;

/// <remarks>
/// An ECS handler always runs on the ECS thread: at once when the game reports an event there, and on the next ECS
/// tick when it reports it from any other thread.
/// </remarks>
internal class MappedEventManager(
    DataSideChannel sideChannel,
    IMappingPolicyDirectory policyDir,
    PendingActionScheduler<CommandBufferSynced> ecsScheduler,
    ILogger logger
) : IMappedEventManager
{
    protected readonly EventQueue incomingEcsEventQueue = new(logger);
    protected readonly EventQueue incomingGameEventQueue = new(logger);

    /// <summary>Runs work on the ECS thread.</summary>
    protected readonly PendingActionScheduler<CommandBufferSynced> ecsScheduler = ecsScheduler;

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

        ecsScheduler.Schedule(static (_, self, ev0) => self.InvokeEcsHandlers(ev0), this, ev);
        return true;
    }

    private void InvokeEcsHandlers<TEvent>(TEvent ev)
        where TEvent : struct
    {
        // Pushed on the ECS thread, where the handlers run, so one of them invoking the event in the game is refused
        using (sideChannel.PushScope<PropagatingToEcsScope<TEvent>>())
        {
            incomingEcsEventQueue.Invoke(ev);
        }
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