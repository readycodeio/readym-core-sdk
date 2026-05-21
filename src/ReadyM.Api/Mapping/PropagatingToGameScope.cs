using System;

namespace ReadyM.Api.Mapping;

internal struct PropagatingToGameScope<TEvent> : IPropagationScope
{
    public PropagationDirection Direction => PropagationDirection.ToGame;
    public Type EventType => typeof(TEvent);
}