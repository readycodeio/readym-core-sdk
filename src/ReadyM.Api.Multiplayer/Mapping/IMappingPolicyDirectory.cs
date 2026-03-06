using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.CreateDestroy;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;

namespace ReadyM.Api.Multiplayer.Mapping;

public interface IMappingPolicyDirectory
{
    IMappingCreateDeletePolicy<TGameObject> ForCreateDelete<TGameObject>(ArchetypeId archetypeId)
        where TGameObject : class;

    IMappingDataPolicy<TContext> ForData<TComponent, TContext>()
        where TComponent : struct, IMappingContext<TContext>;

    IMappingDataPolicy<Entity> ForData<TComponent>()
        where TComponent : struct, IMappingContext<Entity>;

    IMappingEventPolicy<TContext> ForEvent<TEvent, TContext>()
        where TEvent : struct, IMappingContext<TContext>;

    IMappingEventPolicy<Entity> ForEvent<TEvent>()
        where TEvent : struct, IMappingContext<Entity>;
}