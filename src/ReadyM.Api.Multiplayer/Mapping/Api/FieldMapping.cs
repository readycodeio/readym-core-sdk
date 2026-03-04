using System;

namespace ReadyM.Api.Multiplayer.Mapping.Api;

public sealed class FieldMapping<TComponent, TValue>(
    Action<TValue> setToGame,
    DataLoaderDelegate<TComponent, TValue> loadFromGame
)
{
    public void SyncToGame(in TValue value) => setToGame(value);
    public TValue LoadFromGame(ref TComponent component) => loadFromGame(ref component);
}

public sealed class FieldMapping<TComponent, TContext, TValue>(
    Action<TContext, TValue> setToGame,
    DataLoaderDelegate<TComponent, TContext, TValue> loadFromGame
)
{
    public void SyncToGame(TContext context, in TValue value) => setToGame(context, value);
    public TValue LoadFromGame(ref TComponent component, TContext context) => loadFromGame(ref component, context);
}