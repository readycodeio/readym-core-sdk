using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Api.Mapping.Tags;
using ReadyM.SDK.Client.Relay.State;

namespace ReadyM.SDK.Client.Relay.Mapping.Policies;

internal class OwnershipEventPolicyFactory(
    ClientOwnershipManager ownership,
    DataSideChannel sideChannel) : IMappingEventPolicyFactory
{
    public bool Supports(Type eventType, Type contextType)
        => contextType == typeof(Entity) && typeof(IOwnershipBased).IsAssignableFrom(eventType);

    public IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType)
    {
        Debug.Assert(contextType == typeof(Entity));
        var policyType = typeof(OwnershipEventPolicy<>).MakeGenericType(eventType);
        return (IMappingEventPolicyBase)Activator.CreateInstance(policyType, ownership, sideChannel)!;
    }

    public IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType)
    {
        Debug.Assert(typeof(TContext) == typeof(Entity));
        return (IMappingEventPolicy<TContext>)CreatePolicy(eventType, typeof(TContext));
    }
}
