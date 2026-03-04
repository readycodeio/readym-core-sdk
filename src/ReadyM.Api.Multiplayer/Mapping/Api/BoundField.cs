using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.Api;

public readonly struct BoundField<TComponent, TValue>
    where TComponent : IComponent
{
    public readonly int Id;
    public readonly Func<TComponent, TValue> Get;
    public readonly FieldSetterDelegate<TComponent, TValue> Set;

    internal BoundField(int id, Func<TComponent, TValue> get, FieldSetterDelegate<TComponent, TValue> set)
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
    public readonly FieldSetterDelegate<TComponent, TValue> Set;

    internal BoundField(int id, Func<TComponent, TValue> get, FieldSetterDelegate<TComponent, TValue> set)
    {
        Id = id;
        Get = get;
        Set = set;
    }
}