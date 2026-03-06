using System;

namespace ReadyM.Api.Multiplayer.Mapping.Data;

public readonly struct Field<TComponent, TValue>(
    int id,
    Func<TComponent, TValue> get,
    FieldSetterDelegate<TComponent, TValue> setFromGame,
    FieldSetterDelegate<TComponent, TValue> setFromApi,
    Func<TComponent, bool> wasSetFromApi
)
    where TComponent : struct
{
    public readonly int Id = id;
    public readonly Func<TComponent, bool> WasSetFromApi = wasSetFromApi;
    public readonly Func<TComponent, TValue> Get = get;
    public readonly FieldSetterDelegate<TComponent, TValue> SetFromGame = setFromGame;
    public readonly FieldSetterDelegate<TComponent, TValue> SetFromApi = setFromApi;

    public Field<TComponent, TValue, TContext> In<TContext>() => new(Id, Get, SetFromGame, WasSetFromApi);

    public static implicit operator int(Field<TComponent, TValue> field) => field.Id;
}

public readonly struct Field<TComponent, TValue, TContext>(
    int id,
    Func<TComponent, TValue> get,
    FieldSetterDelegate<TComponent, TValue> setFromGame,
    Func<TComponent, bool> wasSetFromApi
)
    where TComponent : struct
{
    public readonly int Id = id;
    public readonly Func<TComponent, bool> WasSetFromApi = wasSetFromApi;
    public readonly Func<TComponent, TValue> Get = get;
    public readonly FieldSetterDelegate<TComponent, TValue> SetFromGame = setFromGame;

    public static implicit operator int(Field<TComponent, TValue, TContext> field) => field.Id;
}