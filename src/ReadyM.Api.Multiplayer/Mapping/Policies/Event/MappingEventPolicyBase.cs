using System;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Events;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event;

public abstract class MappingEventPolicyBase<TEvent, TContext>(DataSideChannel sideChannel) : IMappingEventPolicy<TContext>
    where TContext : struct
{
    public Type ContextType => typeof(TContext);

    /// <inheritdoc cref="CanGameEventNotifyEcsImpl"/>
    /// Always returns <c>false</c> if the event is already propagating to the game, to avoid recursion.
    public bool CanGameEventNotifyEcs(in TContext context)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return false;

        return CanGameEventNotifyEcsImpl(context);
    }

    /// Returns whether the event should be allowed to propagate to the ECS or not, based on the provided context.
    /// This is usually called in patches to game code.
    protected abstract bool CanGameEventNotifyEcsImpl(in TContext context);

    /// <inheritdoc cref="CanEcsInvokeGameEventImpl"/>
    /// Always returns <c>false</c> if the event is already propagating to the ECS, to avoid recursion.
    public bool CanEcsInvokeGameEvent(in TContext context)
    {
        if (sideChannel.HasData<PropagatingToEcsScope<TEvent>>())
            return false;

        return CanEcsInvokeGameEventImpl(context);
    }

    /// Returns whether the event should be allowed to propagate to the game or not, based on the provided context.
    /// This is usually called in network event handlers.
    protected abstract bool CanEcsInvokeGameEventImpl(in TContext context);

    /// Returns whether the game is allowed to run the game code related to the event locally, or not.
    /// This is used exclusively in game code patches, to avoid running events for entities we are not responsible for.
    /// Always returns <c>true</c> if the event is propagating from the ECS to the game, to allow the game code to run for the event.
    public bool CanGameEventRunLocally(in TContext context, out EventSource eventSource)
    {
        eventSource = sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? EventSource.Trigger
            : EventSource.Game;

        if (eventSource == EventSource.Trigger)
            return true;

        return CanGameEventRunLocallyImpl(context);
    }

    /// Returns whether the game is allowed to run the game code related to the event locally, or not.
    /// This is used exclusively in game code patches, to avoid running events for entities we are not responsible for.
    protected abstract bool CanGameEventRunLocallyImpl(in TContext context);
}