using System;

namespace ReadyM.Api.Mapping.Events;

public interface IMappingEventPolicyFactory
{
    bool Supports(Type eventType, Type contextType);
    IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType);
    IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType)
        where TContext : struct;
}