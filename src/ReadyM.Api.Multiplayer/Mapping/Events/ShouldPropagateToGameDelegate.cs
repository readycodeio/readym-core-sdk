namespace ReadyM.Api.Multiplayer.Mapping.Events;

public delegate bool ShouldPropagateToGameDelegate<TContext>(in TContext ev);