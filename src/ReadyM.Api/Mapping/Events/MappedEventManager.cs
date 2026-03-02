using System;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;

namespace ReadyM.Api.Mapping.Events;

public class MappedEventManager(DataSideChannel sideChannel, IMappingPolicyDirectory policyDir, ILogger logger) : IMappedEventManager
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

    [Obsolete("Use PropagateToEcsIfApplicable instead to respect the event policy.")]
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

    /// <inheritdoc/>
    public void PropagateToEcsIfApplicable<TEvent>(in TEvent ev, Entity context) where TEvent : struct, IMappingContext<Entity>
    {
        // do not propagate if we're already propagating this event to game
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return;

        var policy = policyDir.ForEvent<TEvent>().ShouldEventPropagateToEcs(context);
        if (policy)
        {
            PropagateToEcs(ev);
        }
    }

    /// <inheritdoc/>
    public bool ShouldGameRunLocally<TEvent>(Entity context)
        where TEvent : struct, IMappingContext<Entity>
    {
        // if the caller was ran from API, allow
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return true;

        // if the caller was ran from game, check policy
        return policyDir.ForEvent<TEvent>().ShouldGameEventRunLocally(context, out _);
    }
}