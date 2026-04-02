using System;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;

namespace ReadyM.Api.Multiplayer.Mapping.Events;

internal class FuncEntityEventPolicyFactory<TContext>(
    Func<TContext, bool> shouldPropagateToEcs,
    Func<TContext, bool> shouldPropagateToGame,
    ShouldRunLocallyDelegate<TContext> shouldRunLocally)
    : IMappingEventPolicyFactory
{
    public bool Supports(Type eventType, Type contextType)
        => contextType == typeof(TContext);

    public IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType)
    {
        var policyType = typeof(FuncEventPolicy<,>).MakeGenericType(eventType, contextType);
        return (IMappingEventPolicyBase)Activator.CreateInstance(policyType, shouldPropagateToEcs, shouldPropagateToGame, shouldRunLocally)!;
    }

    public IMappingEventPolicy<TCtx> CreatePolicy<TCtx>(Type eventType)
        => (IMappingEventPolicy<TCtx>)CreatePolicy(eventType, typeof(TCtx));
}