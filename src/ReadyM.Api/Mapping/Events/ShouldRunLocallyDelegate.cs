namespace ReadyM.Api.Mapping.Events;

internal delegate bool ShouldRunLocallyDelegate<TContext>(in TContext context);