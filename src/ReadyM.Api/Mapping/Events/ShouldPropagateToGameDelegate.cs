namespace ReadyM.Api.Mapping.Events;

public delegate bool ShouldPropagateToGameDelegate<TContext>(in TContext ev);