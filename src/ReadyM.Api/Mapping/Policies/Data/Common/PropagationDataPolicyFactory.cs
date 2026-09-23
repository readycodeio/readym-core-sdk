using System;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping.Policies.Data.Common;

internal class PropagationDataPolicyFactory(DataSideChannel sideChannel) : IMappingDataPolicyFactory
{
    public bool Supports(Type dataType, Type contextType) => PolicyFor(dataType) is not null;

    public IMappingDataPolicyBase CreatePolicy(Type componentType, Type contextType)
    {
        var policy = PolicyFor(componentType)
                     ?? throw new ArgumentException($"{componentType} does not say how it propagates.");

        return (IMappingDataPolicyBase)Activator.CreateInstance(
            policy.MakeGenericType(componentType, contextType), sideChannel)!;
    }

    public IMappingDataPolicy<TContext> CreatePolicy<TContext>(Type componentType)
        => (IMappingDataPolicy<TContext>)CreatePolicy(componentType, typeof(TContext));

    private static Type? PolicyFor(Type dataType)
    {
        if (typeof(IServerAuthoritative).IsAssignableFrom(dataType))
            return typeof(ServerAuthoritativeDataPolicy<,>);

        if (typeof(IAlwaysPropagates).IsAssignableFrom(dataType))
            return typeof(AlwaysPropagatesDataPolicy<,>);

        if (typeof(IAlwaysPropagatesToEcsOnly).IsAssignableFrom(dataType))
            return typeof(AlwaysPropagatesToEcsOnlyDataPolicy<,>);

        return typeof(IAlwaysPropagatesToGameOnly).IsAssignableFrom(dataType)
            ? typeof(AlwaysPropagatesToGameOnlyDataPolicy<,>)
            : null;
    }
}
