namespace ReadyM.Api.Multiplayer.Mapping.Events;

public delegate bool ShouldRunLocallyDelegate<TContext>(in TContext context);