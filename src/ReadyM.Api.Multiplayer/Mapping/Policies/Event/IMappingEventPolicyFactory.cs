using System;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event;

public interface IMappingEventPolicyFactory
{
    bool Supports(Type eventType, Type contextType);
    IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType);
    IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType);
}