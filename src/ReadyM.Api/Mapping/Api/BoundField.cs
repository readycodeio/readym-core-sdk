using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Api;

public readonly struct BoundField<TComponent, TValue>
    where TComponent : IComponent
{
    public readonly int Id;
    public readonly Func<TComponent, TValue> Get;
    public readonly Action<TComponent, TValue> Set;

    internal BoundField(int id, Func<TComponent, TValue> get, Action<TComponent, TValue> set)
    {
        Id = id;
        Get = get;
        Set = set;
    }
}

public readonly struct BoundField<TComponent, TValue, TContext>
    where TComponent : IComponent
{
    public readonly int Id;
    public readonly Func<TComponent, TValue> Get;
    public readonly Action<TComponent, TValue> Set;

    internal BoundField(int id, Func<TComponent, TValue> get, Action<TComponent, TValue> set)
    {
        Id = id;
        Get = get;
        Set = set;
    }
}