namespace ReadyM.Api.Multiplayer.Mapping.Events;

internal delegate bool ShouldPropagateToGameDelegate<TContext>(in TContext ev);