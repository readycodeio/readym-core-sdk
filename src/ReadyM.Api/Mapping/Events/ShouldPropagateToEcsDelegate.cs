namespace ReadyM.Api.Mapping.Events;

public delegate bool ShouldPropagateToEcsDelegate<TEvent>(in TEvent ev);