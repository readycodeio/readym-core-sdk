namespace ReadyM.SDK.Client.Mapping;

/// Reads a value out of the game and into the shape.
public delegate void Pull<TValue, in TContext>(ref TValue value, TContext context);