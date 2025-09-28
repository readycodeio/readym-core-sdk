namespace ReadyM.Api.Mapping.Events;

public delegate bool ShouldPropagateToGameDelegate<TEvent>(in TEvent ev);