using System;
using System.Diagnostics;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Data.Common;

public class FuncDataPolicyFactory<TContext>(
    Func<TContext, bool> shouldEcsCopyToGame,
    Func<TContext, bool> shouldGameCopyToEcs,
    Func<TContext, bool> shouldSetLocally
)
    : IMappingDataPolicyFactory
{
    public bool Supports(Type dataType, Type contextType)
        => typeof(TContext) == contextType;

    public IMappingDataPolicyBase CreatePolicy(Type componentType, Type contextType)
    {
        Debug.Assert(contextType == typeof(TContext));

        var policyType = typeof(FuncDataPolicy<>).MakeGenericType(contextType);
        var policy = Activator.CreateInstance(policyType, shouldEcsCopyToGame, shouldGameCopyToEcs, shouldSetLocally);
        return (IMappingDataPolicyBase)policy!;
    }

    public IMappingDataPolicy<TCtx> CreatePolicy<TCtx>(Type componentType)
        => (IMappingDataPolicy<TCtx>)CreatePolicy(componentType, typeof(TContext));
}