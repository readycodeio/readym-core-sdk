using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Data;

/// <exclude />
public readonly struct Field<TComponent, TValue>(
    int id,
    Func<TComponent, TValue> get,
    FieldSetterDelegate<TComponent, TValue> set,
    FieldSetterFromApiDelegate<TComponent, TValue> setFromApi,
    Func<TComponent, bool> wasSetFromApi
)
    where TComponent : struct
{
    internal readonly int Id = id;
    internal readonly Func<TComponent, bool> WasSetFromApi = wasSetFromApi;
    internal readonly Func<TComponent, TValue> Get = get;
    internal readonly FieldSetterDelegate<TComponent, TValue> Set = set;
    internal readonly FieldSetterFromApiDelegate<TComponent, TValue> SetFromApi = setFromApi;

    public Field<TComponent, TValue, TContext> In<TContext>() => new(Id, Get, Set, WasSetFromApi);

    public static implicit operator int(Field<TComponent, TValue> field) => field.Id;
}

/// <exclude />
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
