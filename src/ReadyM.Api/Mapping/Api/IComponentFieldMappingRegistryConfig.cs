using System;

namespace ReadyM.Api.Mapping.Api;

public interface IComponentFieldMappingRegistryConfig
{
    BoundField<TComponent, TValue> Register<TComponent, TValue>(
        Field<TComponent, TValue> field,
        Action<TValue> setter,
        Func<TValue> getter)
        where TComponent : struct;

    BoundField<TComponent, TValue, TContext> Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Func<TContext, TValue> getter)
        where TComponent : struct;
}