namespace ReadyM.Api.Multiplayer.Mapping.Events;

internal delegate bool ShouldPropagateToEcsDelegate<TContext>(in TContext ev);