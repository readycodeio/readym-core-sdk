namespace ReadyM.Api.Mapping.Events;

internal delegate bool ShouldPropagateToGameDelegate<TContext>(in TContext ev);