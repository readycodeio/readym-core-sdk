namespace ReadyM.Api.Mapping.Events;

internal delegate bool ShouldPropagateToEcsDelegate<TContext>(in TContext ev);