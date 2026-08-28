using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping.Data;

internal sealed class ComponentFieldMappingRegistry(IMappingPolicyDirectory policyDir, DataSideChannel sideChannel, ILogger logger)
    : IComponentFieldMappingRegistry, IComponentFieldMappingRegistryConfig
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
            field.Set(ref cmp, value);
            return value;
        }
    }

    public void Register<TComponent, TValue, TContext>(
        Field<TComponent, TValue, TContext> field,
        Action<TContext, TComponent> setter,
        Func<TContext, TValue> getter)
        where TComponent : struct, IComponent
    {
        var mapping = new ComponentFieldMapping<TComponent, TContext, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), typeof(TContext), field.Id), mapping);
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
        Action<TContext, TComponent> setter,
        DataLoader<TComponent, TContext> loader)
        where TComponent : struct, IComponent
    {
        var mapping = new ComponentFieldMapping<TComponent, TContext, TValue>(setter, Loader);
        _mappings.Add(new FieldKey(typeof(TComponent), typeof(TContext), field.Id), mapping);
        return;

        TValue Loader(ref TComponent cmp, TContext ctx)
        {
            loader(ref cmp, ctx);
            return field.Get(cmp);
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

    /// <summary>
    /// Looks up the registered mapping without committing to a shape. A field may be registered either as a
    /// <see cref="FieldMapping{TComponent,TContext,TValue}"/>, whose setter takes the field value alone, or as a
    /// <see cref="ComponentFieldMapping{TComponent,TContext,TValue}"/>, whose setter takes the whole component
    /// so it can decide based on sibling fields. Callers must handle both.
    /// </summary>
    private bool TryGetMapping<TComponent, TValue, TContext>(Field<TComponent, TValue, TContext> field, out object? mapping)
        where TComponent : struct, IComponent
        => _mappings.TryGetValue(new FieldKey(typeof(TComponent), typeof(TContext), field.Id), out mapping);

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
            if (!registry.TryGetMapping(field, out var mapping))
            {
                registry.Logger.LogError("Failed to find mapping for component {Component}, field {FieldId} and context {Context}", typeof(TComponent).Name, field.Id, typeof(TContext).Name);
                return;
            }

            ref var component = ref entity.GetComponent<TComponent>();

            if (!fromApi || field.WasSetFromApi(component))
            {
                using (registry.SideChannel.PushScope<PropagatingToGameScope<TComponent>>())
                {
                    switch (mapping)
                    {
                        case FieldMapping<TComponent, TContext, TValue> fieldMapping:
                            fieldMapping.SyncToGame(field.Get(component), context);
                            break;
                        case ComponentFieldMapping<TComponent, TContext, TValue> componentMapping:
                            componentMapping.SyncToGame(component, context);
                            break;
                        default:
                            registry.Logger.LogError("Mapping for component {Component}, field {FieldId} and context {Context} has unexpected type {MappingType}", typeof(TComponent).Name, field.Id, typeof(TContext).Name, mapping?.GetType().Name);
                            return;
                    }

                    component.ClearApiFlag(field.Id);
                }
            }
        }

        public void SyncToGame<TValue, TContext>(Field<TComponent, TValue> field, Action<TValue, TContext> setter, TContext context)
        {
            ref var component = ref entity.GetComponent<TComponent>();

            if (!fromApi || field.WasSetFromApi(component))
            {
                var value = field.Get(component);
                setter(value, context);
                component.ClearApiFlag(field.Id);
            }
        }

        public void SyncToGame<TValue>(Field<TComponent, TValue> field, ref TValue valueRef)
        {
            ref var component = ref entity.GetComponent<TComponent>();

            if (!fromApi || field.WasSetFromApi(component))
            {
                var value = field.Get(component);
                valueRef = value;
                component.ClearApiFlag(field.Id);
            }
        }

        public void SyncToGame<TContext>(Action<TComponent, TContext> setter, TContext context)
        {
            ref var component = ref entity.GetComponent<TComponent>();

            setter(component, context);

            if (fromApi)
            {
                component.ClearApiFlag();
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
            if (!registry.TryGetMapping(field, out var mapping))
            {
                registry.Logger.LogError("Failed to find mapping for component {Component}, field {FieldId} and context {Context}", typeof(TComponent).Name, field.Id, typeof(TContext).Name);
                return;
            }

            using (registry.SideChannel.PushScope<PropagatingToEcsScope<TComponent>>()) // TODO: Is this even necessary?
            {
                ref var component = ref entity.GetComponent<TComponent>();

                // Both shapes carry the same loader, only the direction towards the game differs.
                switch (mapping)
                {
                    case FieldMapping<TComponent, TContext, TValue> fieldMapping:
                        fieldMapping.LoadFromGame(ref component, context);
                        break;
                    case ComponentFieldMapping<TComponent, TContext, TValue> componentMapping:
                        componentMapping.LoadFromGame(ref component, context);
                        break;
                    default:
                        registry.Logger.LogError("Mapping for component {Component}, field {FieldId} and context {Context} has unexpected type {MappingType}", typeof(TComponent).Name, field.Id, typeof(TContext).Name, mapping?.GetType().Name);
                        break;
                }
            }
        }

        public void SetFromGame<TValue>(Field<TComponent, TValue> field, TValue value)
        {
            ref var component = ref entity.GetComponent<TComponent>();
            field.Set(ref component, value);
        }
    }

    public bool CanLoadFromGame<TComponent>(Entity entity, out LoadFromGameHelper<TComponent> fromGameHelper)
        where TComponent : struct, IReadyComponent, IMappingContext<Entity>
    {
        var component = entity.GetComponent<TComponent>();

        if (policyDir.ForData<TComponent, Entity>().CanSetFromApi(entity) && component.ChangedFromApi)
        {
            fromGameHelper = default;
            return false;
        }

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
            field.SetFromApi(ref component, value, entity.Id);
        }
    }

    public bool CanSetFromApi<TComponent>(Entity entity, out SetFromApiHelper<TComponent> fromApiHelper)
        where TComponent : struct, IReadyComponent, IMappingContext<Entity>
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
