using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Relay.Client.State;

namespace ReadyM.Relay.Client.Mapping.Policies;

internal class OwnershipDataPolicyFactory(ClientOwnershipManager ownership, DataSideChannel sideChannel) : IMappingDataPolicyFactory
{
    public bool Supports(Type dataType, Type contextType)
        => contextType == typeof(Entity) && typeof(IOwnershipBased).IsAssignableFrom(dataType);

    public IMappingDataPolicyBase CreatePolicy(Type componentType, Type contextType)
    {
        Debug.Assert(contextType == typeof(Entity), "contextType == typeof(Entity).");
        var genericType = typeof(OwnershipDataPolicy<>).MakeGenericType(componentType);
        return (IMappingDataPolicyBase)Activator.CreateInstance(genericType, ownership, sideChannel)!;
    }

    public IMappingDataPolicy<TContext> CreatePolicy<TContext>(Type componentType)
        => (IMappingDataPolicy<TContext>)CreatePolicy(componentType, typeof(TContext));
}
