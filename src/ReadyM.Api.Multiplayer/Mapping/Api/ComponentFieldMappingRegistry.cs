using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.Api;

public sealed class ComponentFieldMappingRegistry : IComponentFieldMappingRegistry, IComponentFieldMappingRegistryConfig
{
    private readonly Dictionary<FieldKey, object> _mappings = new();

    public BoundField<TComponent, TValue> Register<TComponent, TValue>(
        Field<TComponent, TValue> field,
        Action<TValue> setter,
        Func<TValue> getter)
        where TComponent : IComponent
    {
        var mapping = new FieldMapping<TComponent, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), field.Id), mapping);
        return new BoundField<TComponent, TValue>(field.Id, field.Get, field.Set);

        TValue Loader(ref TComponent cmp)
        {
            var value = getter();
            field.Set(ref cmp, value);
            return value;
        }
    }
    
    public BoundField<TComponent, TValue> Register<TComponent, TValue>(
        Field<TComponent, TValue> field,
        Action<TValue> setter,
        DataLoader<TComponent, TValue> loader)
        where TComponent : IComponent
    {
        var mapping = new FieldMapping<TComponent, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), field.Id), mapping);
        return new BoundField<TComponent, TValue>(field.Id, field.Get, field.Set);
        
        TValue Loader(ref TComponent cmp)
        {
            loader(ref cmp);
            return field.Get(cmp);
        }
    }

    public BoundField<TComponent, TValue, TContext> Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Func<TContext, TValue> getter)
        where TComponent : IComponent
    {
        var mapping = new FieldMapping<TComponent, TContext, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), field.Id), mapping);
        return new BoundField<TComponent, TValue, TContext>(field.Id, field.Get, field.Set);

        TValue Loader(ref TComponent cmp, TContext ctx)
        {
            var value = getter(ctx);
            field.Set(ref cmp, value);
            return value;
        }
    }

    public BoundField<TComponent, TValue, TContext> Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        DataLoader<TComponent, TContext, TValue> loader)
        where TComponent : IComponent
    {
        var mapping = new FieldMapping<TComponent, TContext, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), field.Id), mapping);
        return new BoundField<TComponent, TValue, TContext>(field.Id, field.Get, field.Set);

        TValue Loader(ref TComponent cmp, TContext ctx)
        {
            loader(ref cmp, ctx);
            return field.Get(cmp);
        }
    }

    public FieldMapping<TComponent, TValue> Get<TComponent, TValue>(
        BoundField<TComponent, TValue> field)
        where TComponent : IComponent
    {
        return (FieldMapping<TComponent, TValue>)_mappings[new FieldKey(typeof(TComponent), field.Id)];
    }

    public FieldMapping<TComponent, TContext, TValue> Get<TComponent, TValue, TContext>(
        BoundField<TComponent, TValue, TContext> field)
        where TComponent : IComponent
    {
        return (FieldMapping<TComponent, TContext, TValue>)_mappings[new FieldKey(typeof(TComponent), field.Id)];
    }
}