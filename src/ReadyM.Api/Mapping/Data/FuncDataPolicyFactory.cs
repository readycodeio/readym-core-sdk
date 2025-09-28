using System;
using System.Diagnostics;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Mapping.Data;

public class FuncDataPolicyFactory<TContext>(
    Func<TContext, bool> shouldEcsCopyToGame,
    Func<TContext, bool> shouldGameCopyToEcs,
    Func<TContext, bool> shouldSetLocally)
    : IMappingDataPolicyFactory
{
    public bool Supports(Type dataType, Type contextType)
        => typeof(TContext) == contextType;

    public IMappingDataPolicyBase CreatePolicy(ArchetypeId archetypeId, Type dataType, Type contextType)
    {
        Debug.Assert(contextType == typeof(TContext));
        
        var policyType = typeof(FuncDataPolicy<>).MakeGenericType(contextType);
        var policy = Activator.CreateInstance(policyType, shouldEcsCopyToGame, shouldGameCopyToEcs, shouldSetLocally);
        return (IMappingDataPolicyBase)policy!;
    }

    public IMappingDataPolicy<TCtx> CreatePolicy<TCtx>(ArchetypeId archetypeId, Type dataType)
        where TCtx : struct
        => (IMappingDataPolicy<TCtx>)CreatePolicy(archetypeId, dataType, typeof(TContext));
}