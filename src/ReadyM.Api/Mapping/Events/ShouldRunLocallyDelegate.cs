namespace ReadyM.Api.Mapping.Events;

public delegate bool ShouldRunLocallyDelegate<TContext>(in TContext context);