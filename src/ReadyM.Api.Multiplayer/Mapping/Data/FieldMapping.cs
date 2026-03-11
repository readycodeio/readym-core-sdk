using System;

namespace ReadyM.Api.Multiplayer.Mapping.Data;

public readonly struct FieldMapping<TComponent, TContext, TValue>(
    Action<TContext, TValue> setToGame,
    DataLoaderDelegate<TComponent, TContext, TValue> loadFromGame
)
    where TComponent : struct
{
    public void SyncToGame(in TValue value, TContext context) => setToGame(context, value);
    public TValue LoadFromGame(ref TComponent component, TContext context) => loadFromGame(ref component, context);
}

public readonly struct ComponentFieldMapping<TComponent, TContext, TValue>(
    Action<TContext, TComponent> setToGame,
    DataLoaderDelegate<TComponent, TContext, TValue> loadFromGame
)
    where TComponent : struct
{
    public void SyncToGame(in TComponent value, TContext context) => setToGame(context, value);
    public TValue LoadFromGame(ref TComponent component, TContext context) => loadFromGame(ref component, context);
}