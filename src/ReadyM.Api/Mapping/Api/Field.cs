using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Api;

public readonly struct Field<TComponent, TValue>(
    int id,
    Func<TComponent, TValue> get,
    Action<TComponent, TValue> set
)
    where TComponent : IComponent
{
    public readonly int Id = id;
    public readonly Func<TComponent, TValue> Get = get;
    public readonly Action<TComponent, TValue> Set = set;

    public Field<TComponent, TValue, TContext> In<TContext>() => new(Id, Get, Set);

    public static implicit operator int(Field<TComponent, TValue> field) => field.Id;
}

public readonly struct Field<TComponent, TValue, TContext>(
    int id,
    Func<TComponent, TValue> get,
    Action<TComponent, TValue> set
)
    where TComponent : IComponent
{
    public readonly int Id = id;
    public readonly Func<TComponent, TValue> Get = get;
    public readonly Action<TComponent, TValue> Set = set;
}