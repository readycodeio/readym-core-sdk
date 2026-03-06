using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.Data;

public interface IComponentFieldMappingRegistry
{
    bool CanSyncToGame<TComponent>(Entity entity, out ComponentFieldMappingRegistry.SyncToGameHelper<TComponent> toGameHelper)
        where TComponent : struct, IReadyComponent, IMappingContext<Entity>;

    bool CanLoadFromGame<TComponent>(Entity entity, out ComponentFieldMappingRegistry.LoadFromGameHelper<TComponent> fromGameHelper)
        where TComponent : struct, IComponent, IMappingContext<Entity>;

    bool CanSetFromApi<TComponent>(Entity entity, out ComponentFieldMappingRegistry.SetFromApiHelper<TComponent> fromApiHelper)
        where TComponent : struct, IComponent, IMappingContext<Entity>;
}