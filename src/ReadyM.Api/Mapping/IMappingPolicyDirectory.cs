using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.CreateDestroy;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.Mapping;

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