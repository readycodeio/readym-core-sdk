using System;
using ReadyM.Api.Helpers;

namespace ReadyM.Api.Mapping.Events;

public abstract class MappingEventPolicyBase<TEvent, TContext>(DataSideChannel sideChannel) : IMappingEventPolicy<TContext>
    where TContext : struct
{
    public Type ContextType => typeof(TContext);

    /// <inheritdoc cref="ShouldEventPropagateToEcsImpl"/>
    /// Always returns <c>false</c> if the event is already propagating to the game, to avoid recursion.
    public bool ShouldEventPropagateToEcs(in TContext context)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return false;

        return ShouldEventPropagateToEcsImpl(context);
    }

    /// Returns whether the event should be allowed to propagate to the ECS or not, based on the provided context.
    /// This is usually called in patches to game code.
    protected abstract bool ShouldEventPropagateToEcsImpl(in TContext context);

    /// <inheritdoc cref="ShouldEventPropagateToGameImpl"/>
    /// Always returns <c>false</c> if the event is already propagating to the ECS, to avoid recursion.
    [Obsolete("Is this event needed in the API?")]
    public bool ShouldEventPropagateToGame(in TContext context)
    {
        if (sideChannel.HasData<PropagatingToEcsScope<TEvent>>())
            return false;

        return ShouldEventPropagateToGameImpl(context);
    }

    /// Returns whether the event should be allowed to propagate to the game or not, based on the provided context.
    /// This is usually called in network event handlers.
    [Obsolete("Is this event needed in the API?")]
    protected abstract bool ShouldEventPropagateToGameImpl(in TContext context);

    /// Returns whether the game is allowed to run the game code related to the event locally, or not.
    /// This is used exclusively in game code patches, to avoid running events for entities we are not responsible for.
    /// Always returns <c>true</c> if the event is propagating from the ECS to the game, to allow the game code to run for the event.
    public bool ShouldGameEventRunLocally(in TContext context, out EventSource eventSource)
    {
        eventSource = sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? EventSource.Trigger
            : EventSource.Game;

        if (eventSource == EventSource.Trigger)
            return true;

        return ShouldGameEventRunLocallyImpl(context);
    }

    /// Returns whether the game is allowed to run the game code related to the event locally, or not.
    /// This is used exclusively in game code patches, to avoid running events for entities we are not responsible for.
    protected abstract bool ShouldGameEventRunLocallyImpl(in TContext context);
}