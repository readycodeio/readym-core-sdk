using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Events;

namespace ReadyM.Api.Multiplayer.Mapping.Data;

public sealed class ComponentFieldMappingRegistry(IMappingPolicyDirectory policyDir, DataSideChannel sideChannel, ILogger logger) : IComponentFieldMappingRegistry, IComponentFieldMappingRegistryConfig
{
    private DataSideChannel SideChannel => sideChannel;
    private ILogger Logger => logger;
    private readonly Dictionary<FieldKey, object> _mappings = [];

    public void Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TValue> setter,
        Func<TContext, TValue> getter)
        where TComponent : struct, IComponent
    {
        var mapping = new FieldMapping<TComponent, TContext, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), typeof(TContext), field.Id), mapping);
        return;

        TValue Loader(ref TComponent cmp, TContext ctx)
        {
            var value = getter(ctx);
            field.SetFromGame(ref cmp, value);
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
        _mappings.Add(new FieldKey(typeof(TComponent), typeof(TContext), field.Id), mapping);
        return;

        TValue Loader(ref TComponent cmp, TContext ctx)
        {
            loader(ref cmp, ctx);
            return field.Get(cmp);
        }
    }

    private bool TryGet<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        out FieldMapping<TComponent, TContext, TValue> mapping)
        where TComponent : struct, IComponent
    {
        if (_mappings.TryGetValue(new FieldKey(typeof(TComponent), typeof(TContext), field.Id), out var map))
        {
            mapping = (FieldMapping<TComponent, TContext, TValue>)map;
            return true;
        }

        mapping = default;
        return false;
    }

    public readonly ref struct SyncToGameHelper<TComponent>
        where TComponent : struct, IReadyComponent, IMappingContext<Entity>
    {
        private readonly ComponentFieldMappingRegistry registry;
        private readonly Entity entity;
        private readonly bool fromApi;

        internal SyncToGameHelper(ComponentFieldMappingRegistry registry, Entity entity, bool fromApi)
        {
            this.registry = registry;
            this.entity = entity;
            this.fromApi = fromApi;
        }

        public void SyncToGame<TValue, TContext>(Field<TComponent, TValue, TContext> field, TContext context)
        {
            if (!registry.TryGet(field, out var mapping))
                registry.Logger.LogError("Failed to find mapping for component {Component}, field {FieldId} and context {Context}", typeof(TComponent).Name, field.Id, typeof(TContext).Name);

            ref var component = ref entity.GetComponent<TComponent>();

            if (!fromApi || field.WasSetFromApi(component))
            {
                using (registry.SideChannel.PushScope<PropagatingToGameScope<TComponent>>())
                {
                    var value = field.Get(component);
                    mapping.SyncToGame(value, context);
                    component.ClearApiFlag(field.Id);
                }
            }
        }
    }

    public bool CanSyncToGame<TComponent>(Entity entity, out SyncToGameHelper<TComponent> toGameHelper)
        where TComponent : struct, IReadyComponent, IMappingContext<Entity>
    {
        var component = entity.GetComponent<TComponent>();

        if (policyDir.ForData<TComponent, Entity>().ShouldEcsCopyToGame(entity))
        {
            toGameHelper = new SyncToGameHelper<TComponent>(this, entity, false);
            return true;
        }

        if (policyDir.ForData<TComponent, Entity>().CanSetFromApi(entity) && component.ChangedFromApi)
        {
            toGameHelper = new SyncToGameHelper<TComponent>(this, entity, true);
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
            if (!registry.TryGet(field, out var mapping))
                registry.Logger.LogError("Failed to find mapping for component {Component}, field {FieldId} and context {Context}", typeof(TComponent).Name, field.Id, typeof(TContext).Name);

            using (registry.SideChannel.PushScope<PropagatingToEcsScope<TComponent>>()) // TODO: Is this even necessary?
            {
                ref var component = ref entity.GetComponent<TComponent>();
                mapping.LoadFromGame(ref component, context);
            }
        }

        public void SetFromGame<TValue>(Field<TComponent, TValue> field, TValue value)
        {
            ref var component = ref entity.GetComponent<TComponent>();
            field.SetFromGame(ref component, value);
        }
    }

    public bool CanLoadFromGame<TComponent>(Entity entity, out LoadFromGameHelper<TComponent> fromGameHelper) where TComponent : struct, IComponent, IMappingContext<Entity>
    {
        if (policyDir.ForData<TComponent, Entity>().ShouldGameCopyToEcs(entity))
        {
            fromGameHelper = new LoadFromGameHelper<TComponent>(this, entity);
            return true;
        }

        fromGameHelper = default;
        return false;
    }

    public readonly ref struct SetFromApiHelper<TComponent>
        where TComponent : struct, IComponent
    {
        private readonly Entity entity;

        internal SetFromApiHelper(Entity entity)
        {
            this.entity = entity;
        }

        public void SetFromApi<TValue>(Field<TComponent, TValue> field, TValue value)
        {
            ref var component = ref entity.GetComponent<TComponent>();
            field.SetFromApi(ref component, value);
        }
    }

    public bool CanSetFromApi<TComponent>(Entity entity, out SetFromApiHelper<TComponent> fromApiHelper) where TComponent : struct, IComponent, IMappingContext<Entity>
    {
        if (policyDir.ForData<TComponent, Entity>().CanSetFromApi(entity))
        {
            fromApiHelper = new SetFromApiHelper<TComponent>(entity);
            return true;
        }

        fromApiHelper = default;
        return false;
    }
}