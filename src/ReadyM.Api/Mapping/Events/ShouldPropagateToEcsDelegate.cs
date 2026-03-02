namespace ReadyM.Api.Mapping.Events;

public delegate bool ShouldPropagateToEcsDelegate<TContext>(in TContext ev);