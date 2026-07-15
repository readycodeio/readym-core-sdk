using System;
using System.Diagnostics;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event.Common;

internal class AlwaysPropagatesEventPolicyFactory(
    DataSideChannel sideChannel) : IMappingEventPolicyFactory
{
    public bool Supports(Type eventType, Type contextType)
        => contextType == typeof(EmptyContext) && (
            typeof(IAlwaysPropagates).IsAssignableFrom(eventType) ||
            typeof(IAlwaysPropagatesToEcsOnly).IsAssignableFrom(eventType) ||
            typeof(IAlwaysPropagatesToGameOnly).IsAssignableFrom(eventType)
        );

    public IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType)
    {
        Debug.Assert(contextType == typeof(EmptyContext), "contextType == typeof(EmptyContext)");
        var policyType = typeof(IAlwaysPropagates).IsAssignableFrom(eventType)
            ? typeof(AlwaysPropagatesEventPolicy<>).MakeGenericType(eventType)
            : typeof(IAlwaysPropagatesToEcsOnly).IsAssignableFrom(eventType) 
                ? typeof(AlwaysPropagatesToEcsOnlyEventPolicy<>).MakeGenericType(eventType)
                : typeof(AlwaysPropagatesToGameOnlyEventPolicy<>).MakeGenericType(eventType);
        return (IMappingEventPolicyBase)Activator.CreateInstance(policyType, sideChannel);
    }

    public IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType)
    {
        Debug.Assert(typeof(TContext) == typeof(EmptyContext), "typeof(TContext) == typeof(EmptyContext)");
        return (IMappingEventPolicy<TContext>)CreatePolicy(eventType, typeof(TContext));
    }
}