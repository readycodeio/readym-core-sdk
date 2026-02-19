using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Api;

public interface IComponentFieldMappingRegistry
{
    FieldMapping<TComponent, TValue> Get<TComponent, TValue>(
        BoundField<TComponent, TValue> field)
        where TComponent : IComponent;

    FieldMapping<TComponent, TContext, TValue> Get<TComponent, TValue, TContext>(
        BoundField<TComponent, TValue, TContext> field)
        where TComponent : IComponent;
}