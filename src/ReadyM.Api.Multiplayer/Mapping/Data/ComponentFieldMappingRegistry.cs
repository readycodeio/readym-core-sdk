using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Events;

namespace ReadyM.Api.Multiplayer.Mapping.Data;

public sealed class ComponentFieldMappingRegistry(IMappingPolicyDirectory policyDir, DataSideChannel sideChannel) : IComponentFieldMappingRegistry, IComponentFieldMappingRegistryConfig
{
    private DataSideChannel SideChannel => sideChannel;
    private readonly Dictionary<FieldKey, object> _mappings = [];

    public void Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Func<TContext, TValue> getter)
        where TComponent : struct, IComponent
    {
        var mapping = new FieldMapping<TComponent, TContext, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), field.Id), mapping);
        return;

        TValue Loader(ref TComponent cmp, TContext ctx)
        {
            var value = getter(ctx);
            field.Set(ref cmp, value);
            return value;
        }
    }

    public void Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        DataLoader<TComponent, TContext> loader)
        where TComponent : struct, IComponent
    {
        var mapping = new FieldMapping<TComponent, TContext, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), field.Id), mapping);
        return;

        TValue Loader(ref TComponent cmp, TContext ctx)
        {
            loader(ref cmp, ctx);
            return field.Get(cmp);
        }
    }

    private FieldMapping<TComponent, TContext, TValue> Get<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field)
        where TComponent : struct, IComponent
    {
        return (FieldMapping<TComponent, TContext, TValue>)_mappings[new FieldKey(typeof(TComponent), field.Id)];
    }

    public readonly ref struct SyncToGameHelper<TComponent>
        where TComponent : struct, IComponent
    {
        private readonly ComponentFieldMappingRegistry registry;
        private readonly TComponent component;

        internal SyncToGameHelper(ComponentFieldMappingRegistry registry, TComponent component)
        {
            this.registry = registry;
            this.component = component;
        }

        public void SyncToGame<TValue, TContext>(Field<TComponent, TValue, TContext> field, TContext context)
        {
            var mapping = registry.Get(field);
            using (registry.SideChannel.PushScope<PropagatingToGameScope<TComponent>>())
            {
                mapping.SyncToGame(field.Get(component), context);
            }
        }
    }

    public bool CanSyncToGame<TComponent>(Entity entity, out SyncToGameHelper<TComponent> toGameHelper)
        where TComponent : struct, IComponent, IMappingContext<Entity>
    {
        if (policyDir.ForData<TComponent, Entity>().ShouldEcsCopyToGame(entity))
        {
            var component = entity.GetComponent<TComponent>();
            toGameHelper = new SyncToGameHelper<TComponent>(this, component);
            return true;
        }

        toGameHelper = default;
        return false;
    }

    public readonly ref struct LoadFromGameHelper<TComponent>
        where TComponent : struct, IComponent
    {
        private readonly ComponentFieldMappingRegistry registry;
        private readonly Entity entity;

        internal LoadFromGameHelper(ComponentFieldMappingRegistry registry, Entity entity)
        {
            this.registry = registry;
            this.entity = entity;
        }

        public void LoadFromGame<TValue, TContext>(Field<TComponent, TValue, TContext> field, TContext context)
        {
            var mapping = registry.Get(field);
            using (registry.SideChannel.PushScope<PropagatingToEcsScope<TComponent>>()) // TODO: Is this even necessary?
            {
                ref var component = ref entity.GetComponent<TComponent>();
                mapping.LoadFromGame(ref component, context);
            }
        }
    }

    public bool CanLoadFromGame<TComponent>(Entity entity, out LoadFromGameHelper<TComponent> fromGameHelper) where TComponent : struct, IComponent, IMappingContext<Entity>
    {
        if (policyDir.ForData<TComponent, Entity>().ShouldEcsCopyToGame(entity))
        {
            fromGameHelper = new LoadFromGameHelper<TComponent>(this, entity);
            return true;
        }

        fromGameHelper = default;
        return false;
    }
}