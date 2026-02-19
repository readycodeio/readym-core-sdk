using System;

namespace ReadyM.Api.Mapping.Api;

public sealed class FieldMapping<TValue>(
    Action<TValue> setToGame,
    Func<TValue> getFromGame
)
{
    public void SyncToGame(TValue value) => setToGame(value);
    public TValue GetValueFromGame() => getFromGame();
}

public sealed class FieldMapping<TContext, TValue>(
    Action<TContext, TValue> setToGame,
    Func<TContext, TValue> getFromGame
)
{
    public void SyncToGame(TContext context, TValue value) => setToGame(context, value);
    public TValue GetValueFromGame(TContext context) => getFromGame(context);
}