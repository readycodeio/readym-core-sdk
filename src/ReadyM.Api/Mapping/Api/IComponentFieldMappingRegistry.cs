namespace ReadyM.Api.Mapping.Api;

public interface IComponentFieldMappingRegistry
{
    FieldMapping<TValue> Get<TComponent, TValue>(
        BoundField<TComponent, TValue> field)
        where TComponent : struct;

    FieldMapping<TContext, TValue> Get<TComponent, TValue, TContext>(
        BoundField<TComponent, TValue, TContext> field)
        where TComponent : struct;
}