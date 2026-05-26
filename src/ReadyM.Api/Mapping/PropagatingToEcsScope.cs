using System;

namespace ReadyM.Api.Mapping;

internal struct PropagatingToEcsScope<TEvent> : IPropagationScope
{
    public PropagationDirection Direction => PropagationDirection.ToEcs;
    public Type EventType => typeof(TEvent);
}