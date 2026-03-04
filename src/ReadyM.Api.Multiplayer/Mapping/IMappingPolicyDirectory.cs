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

    IMappingDataPolicy<TContext> ForData<TData, TContext>(ArchetypeId archetypeId)
        where TData : struct, IMappingContext<TContext>
        where TContext : struct;

    IMappingDataPolicy<Entity> ForData<TData>(ArchetypeId archetypeId)
        where TData : struct, IMappingContext<Entity>;

    IMappingEventPolicy<TContext> ForEvent<TEvent, TContext>()
        where TEvent : struct, IMappingContext<TContext>
        where TContext : struct;
    IMappingEventPolicy<Entity> ForEvent<TEvent>()
        where TEvent : struct, IMappingContext<Entity>;
}