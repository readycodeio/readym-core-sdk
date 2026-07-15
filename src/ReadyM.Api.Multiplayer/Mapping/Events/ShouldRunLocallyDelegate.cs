namespace ReadyM.Api.Multiplayer.Mapping.Events;

internal delegate bool ShouldRunLocallyDelegate<TContext>(in TContext context);