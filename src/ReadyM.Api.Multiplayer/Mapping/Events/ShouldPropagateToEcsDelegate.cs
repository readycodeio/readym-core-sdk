namespace ReadyM.Api.Multiplayer.Mapping.Events;

public delegate bool ShouldPropagateToEcsDelegate<TContext>(in TContext ev);