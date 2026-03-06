using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.CreateDestroy;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;

namespace ReadyM.Api.Multiplayer.Mapping;

public interface IMappingPolicyDirectoryRegistration
{
    void RegisterDefaultCreateDelete(IMappingCreateDeletePolicyFactory factory);

    void RegisterDefaultCreateDelete(ArchetypeId archetypeId, IMappingCreateDeletePolicyFactory factory);

    void RegisterDefaultCreateDelete<TGameObject>(
        Func<TGameObject, bool> shouldCreatePropagate,
        Func<Entity, bool> shouldDeletePropagate)
        where TGameObject : class;

    void RegisterCreateDelete<TGameObject>(ArchetypeId archetypeId, IMappingCreateDeletePolicy<TGameObject> policy)
        where TGameObject : class;

    void RegisterCreateDelete<TGameObject>(
        ArchetypeId archetypeId,
        Func<TGameObject, bool> shouldCreatePropagate,
        Func<Entity, bool> shouldDeletePropagate)
        where TGameObject : class;

    void RegisterDefaultData(IMappingDataPolicyFactory factory);

    void RegisterDefaultData<TContext>(
        Func<TContext, bool> shouldEcsCopyToGame,
        Func<TContext, bool> canSetFromApi,
        Func<TContext, bool> shouldGameCopyToEcs,
        Func<TContext, bool> shouldSetLocally);
    
    void RegisterData<TComponent, TContext>(IMappingDataPolicy<TContext> policy)
        where TComponent : IMappingContext<TContext>
        where TContext : struct;

    void RegisterData<TComponent>(
        Func<Entity, bool> shouldEcsCopyToGame,
        Func<Entity, bool> canSetFromApi,
        Func<Entity, bool> shouldGameCopyToEcs,
        Func<Entity, bool> shouldRunLocally)
        where TComponent : IMappingContext<Entity>;

    void RegisterDefaultEvent(IMappingEventPolicyFactory factory);

    void RegisterDefaultEvent<TContext>(
        Func<TContext, bool> shouldGameEventPropagate,
        Func<TContext, bool> shouldEcsEventPropagate,
        ShouldRunLocallyDelegate<TContext> shouldRunLocally);

    void RegisterEvent<TEvent, TContext>(IMappingEventPolicy<TContext> policy)
        where TEvent : struct, IEquatable<TEvent>
        where TContext : struct;

    void RegisterEvent<TEvent, TContext>(
        ShouldPropagateToEcsDelegate<TContext> shouldPropagateToEcs,
        ShouldPropagateToGameDelegate<TContext> shouldPropagateToGame,
        ShouldRunLocallyDelegate<TContext> shouldRunLocally)
        where TEvent : struct, IEquatable<TEvent>
        where TContext : struct;

    void RegisterEvent<TEvent>(
        ShouldPropagateToEcsDelegate<Entity> shouldPropagateToEcs,
        ShouldPropagateToGameDelegate<Entity> shouldPropagateToGame,
        ShouldRunLocallyDelegate<Entity> shouldRunLocally)
        where TEvent : struct, IEquatable<TEvent>;
}