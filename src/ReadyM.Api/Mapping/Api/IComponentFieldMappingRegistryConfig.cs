using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Api;

public delegate void DataLoader<TComponent, out TValue>(ref TComponent component);

public delegate void DataLoader<TComponent, in TContext, out TValue>(ref TComponent component, TContext ctx);

public interface IComponentFieldMappingRegistryConfig
{
    BoundField<TComponent, TValue> Register<TComponent, TValue>(
        Field<TComponent, TValue> field,
        Action<TValue> setter,
        Func<TValue> getter)
        where TComponent : IComponent;
    
    BoundField<TComponent, TValue> Register<TComponent, TValue>(
        Field<TComponent, TValue> field,
        Action<TValue> setter,
        DataLoader<TComponent, TValue> loader)
        where TComponent : IComponent;

    BoundField<TComponent, TValue, TContext> Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Func<TContext, TValue> getter)
        where TComponent : IComponent;

    BoundField<TComponent, TValue, TContext> Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        DataLoader<TComponent, TContext, TValue> loader)
        where TComponent : IComponent;
}