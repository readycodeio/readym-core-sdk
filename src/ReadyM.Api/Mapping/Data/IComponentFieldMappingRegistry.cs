using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping.Data;

internal interface IComponentFieldMappingRegistry
{
    bool CanSyncToGame<TComponent>(Entity entity, out ComponentFieldMappingRegistry.SyncToGameHelper<TComponent> toGameHelper)
        where TComponent : struct, IReadyComponent, IMappingContext<Entity>;

    bool CanLoadFromGame<TComponent>(Entity entity, out ComponentFieldMappingRegistry.LoadFromGameHelper<TComponent> fromGameHelper)
        where TComponent : struct, IComponent, IMappingContext<Entity>;

    bool CanSetFromApi<TComponent>(Entity entity, out ComponentFieldMappingRegistry.SetFromApiHelper<TComponent> fromApiHelper)
        where TComponent : struct, IComponent, IMappingContext<Entity>;
}