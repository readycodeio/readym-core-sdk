using System;

namespace ReadyM.Api.Multiplayer.Mapping.Data;

public readonly struct Field<TComponent, TValue>(
    int id,
    Func<TComponent, TValue> get,
    FieldSetterDelegate<TComponent, TValue> set,
    FieldSetterDelegate<TComponent, TValue> setFromApi,
    Func<TComponent, bool> wasSetFromApi
)
    where TComponent : struct
{
    internal readonly int Id = id;
    internal readonly Func<TComponent, bool> WasSetFromApi = wasSetFromApi;
    internal readonly Func<TComponent, TValue> Get = get;
    internal readonly FieldSetterDelegate<TComponent, TValue> Set = set;
    internal readonly FieldSetterDelegate<TComponent, TValue> SetFromApi = setFromApi;

    public Field<TComponent, TValue, TContext> In<TContext>() => new(Id, Get, Set, WasSetFromApi);

    public static implicit operator int(Field<TComponent, TValue> field) => field.Id;
}

public readonly struct Field<TComponent, TValue, TContext>(
    int id,
    Func<TComponent, TValue> get,
    FieldSetterDelegate<TComponent, TValue> set,
    Func<TComponent, bool> wasSetFromApi
)
    where TComponent : struct
{
    internal readonly int Id = id;
    internal readonly Func<TComponent, bool> WasSetFromApi = wasSetFromApi;
    internal readonly Func<TComponent, TValue> Get = get;
    internal readonly FieldSetterDelegate<TComponent, TValue> Set = set;

    public static implicit operator int(Field<TComponent, TValue, TContext> field) => field.Id;
}