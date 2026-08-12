using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.CreateDestroy;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping;

internal interface IMappingPolicyDirectory
{
    IMappingCreateDeletePolicy<TGameObject> ForCreateDelete<TGameObject>(ArchetypeId archetypeId)
        where TGameObject : class;

    IMappingDataPolicy<TContext> ForData<TComponent, TContext>()
        where TComponent : struct, IMappingContext<TContext>;

    IMappingDataPolicy<Entity> ForData<TComponent>()
        where TComponent : struct, IMappingContext<Entity>;
    
    IMappingDataPolicy<Entity> ForData(Type componentType);

    IMappingEventPolicy<TContext> ForEvent<TEvent, TContext>()
        where TEvent : struct, IMappingContext<TContext>;
    
    IMappingEventPolicy<TContext> ForEvent<TContext>(Type eventType);

    IMappingEventPolicy<Entity> ForEvent<TEvent>()
        where TEvent : struct, IMappingContext<Entity>;

    IMappingEventPolicy<Entity> ForEvent(Type eventType);
}