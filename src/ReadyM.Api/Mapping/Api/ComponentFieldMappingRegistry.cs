using System;
using System.Collections.Generic;

namespace ReadyM.Api.Mapping.Api;

public sealed class ComponentFieldMappingRegistry : IComponentFieldMappingRegistry, IComponentFieldMappingRegistryConfig
{
    private readonly Dictionary<FieldKey, object> _mappings = new();

    public BoundField<TComponent, TValue> Register<TComponent, TValue>(
        Field<TComponent, TValue> field,
        Action<TValue> setter,
        Func<TValue> getter)
        where TComponent : struct
    {
        var mapping = new FieldMapping<TValue>(setter, getter);
        _mappings.Add(new FieldKey(typeof(TComponent), field.Id), mapping);
        return new BoundField<TComponent, TValue>(field.Id);
    }

    public BoundField<TComponent, TValue, TContext> Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Func<TContext, TValue> getter)
        where TComponent : struct
    {
        var mapping = new FieldMapping<TContext, TValue>(setter, getter);
        _mappings.Add(new FieldKey(typeof(TComponent), field.Id), mapping);
        return new BoundField<TComponent, TValue, TContext>(field.Id);
    }
    
    public FieldMapping<TValue> Get<TComponent, TValue>(
            BoundField<TComponent, TValue> field)
            where TComponent : struct
        {
            return (FieldMapping<TValue>)_mappings[new FieldKey(typeof(TComponent), field.Id)];
        }
    
    public FieldMapping<TContext, TValue> Get<TComponent, TValue, TContext>(
        BoundField<TComponent, TValue, TContext> field)
        where TComponent : struct
    {
        return (FieldMapping<TContext, TValue>)_mappings[new FieldKey(typeof(TComponent), field.Id)];
    }
}