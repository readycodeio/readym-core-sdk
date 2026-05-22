using System;

namespace ReadyM.Api.Mapping.Policies.Event;

internal interface IMappingEventPolicyFactory
{
    bool Supports(Type eventType, Type contextType);
    IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType);
    IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType);
}