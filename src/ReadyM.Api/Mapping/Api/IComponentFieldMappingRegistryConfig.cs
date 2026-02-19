using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Api;

public interface IComponentFieldMappingRegistryConfig
{
    BoundField<TComponent, TValue> Register<TComponent, TValue>(
        Field<TComponent, TValue> field,
        Action<TValue> setter,
        Func<TComponent, TValue> getter)
        where TComponent : IComponent;

    BoundField<TComponent, TValue, TContext> Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Action<TComponent, TContext> loader)
        where TComponent : IComponent;

    BoundField<TComponent, TValue, TContext> Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Func<TContext, TValue> getter)
        where TComponent : IComponent;
}