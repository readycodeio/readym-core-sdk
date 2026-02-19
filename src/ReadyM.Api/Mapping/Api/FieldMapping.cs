using System;

namespace ReadyM.Api.Mapping.Api;

public sealed class FieldMapping<TComponent, TValue>(
    Action<TValue> setToGame,
    Func<TComponent, TValue> loadFromGame
)
{
    public void SyncToGame(TValue value) => setToGame(value);
    public TValue LoadFromGame(TComponent component) => loadFromGame(component);
}

public sealed class FieldMapping<TComponent, TContext, TValue>(
    Action<TContext, TValue> setToGame,
    Func<TComponent, TContext, TValue> loadFromGame
)
{
    public void SyncToGame(TContext context, TValue value) => setToGame(context, value);
    public TValue LoadFromGame(TComponent component, TContext context) => loadFromGame(component, context);
}