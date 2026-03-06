using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.Data;

public delegate void DataLoader<TComponent, in TContext>(ref TComponent component, TContext ctx);

public interface IComponentFieldMappingRegistryConfig
{
    void Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Func<TContext, TValue> getter)
        where TComponent : struct, IComponent;

    void Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        DataLoader<TComponent, TContext> loader)
        where TComponent : struct, IComponent;
}