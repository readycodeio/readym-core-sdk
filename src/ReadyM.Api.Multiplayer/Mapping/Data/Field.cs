using System;

namespace ReadyM.Api.Multiplayer.Mapping.Data;

public readonly struct Field<TComponent, TValue>(
    int id,
    Func<TComponent, TValue> get,
    FieldSetterDelegate<TComponent, TValue> set,
    Func<TComponent, bool> wasSetFromApi
)
    where TComponent : struct
{
    public readonly int Id = id;
    public readonly Func<TComponent, bool> WasSetFromApi = wasSetFromApi;
    public readonly Func<TComponent, TValue> Get = get;
    public readonly FieldSetterDelegate<TComponent, TValue> Set = set;

    public Field<TComponent, TValue, TContext> In<TContext> () => new(Id, Get, Set, WasSetFromApi);

    public static implicit operator int(Field<TComponent, TValue> field) => field.Id;
}

public readonly struct Field<TComponent, TValue, TContext>( // TODO: Cannot bind this
    int id,
    Func<TComponent, TValue> get,
    FieldSetterDelegate<TComponent, TValue> set,
    Func<TComponent, bool> wasSetFromApi
)
    where TComponent : struct
{
    public readonly int Id = id;
    public readonly Func<TComponent, bool> WasSetFromApi = wasSetFromApi;
    public readonly Func<TComponent, TValue> Get = get;
    public readonly FieldSetterDelegate<TComponent, TValue> Set = set;
}