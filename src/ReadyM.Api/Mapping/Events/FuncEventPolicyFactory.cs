using System;

namespace ReadyM.Api.Mapping.Events;

public class FuncEntityEventPolicyFactory<TContext>(
    Func<TContext, bool> shouldPropagateToEcs,
    Func<TContext, bool> shouldPropagateToGame,
    ShouldRunLocallyDelegate<TContext> shouldRunLocally)
    : IMappingEventPolicyFactory
{
    public bool Supports(Type eventType, Type contextType)
        => contextType == typeof(TContext);

    public IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType)
    {
        var policyType = typeof(FuncEventPolicy<>).MakeGenericType(contextType);
        return (IMappingEventPolicyBase)Activator.CreateInstance(policyType, shouldPropagateToEcs, shouldPropagateToGame, shouldRunLocally)!;
    }

    public IMappingEventPolicy<TCtx> CreatePolicy<TCtx>(Type eventType)
        where TCtx : struct
        => (IMappingEventPolicy<TCtx>)CreatePolicy(eventType, typeof(TCtx));
}