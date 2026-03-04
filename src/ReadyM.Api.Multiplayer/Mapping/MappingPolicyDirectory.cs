using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.CreateDestroy;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data.Common;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;

namespace ReadyM.Api.Multiplayer.Mapping;

public class MappingPolicyDirectory(DataSideChannel sideChannel) : IMappingPolicyDirectory
{
    private readonly object _createDeleteLock = new();
    private readonly Dictionary<(ArchetypeId, Type), IMappingCreateDeletePolicyBase> _createDeletePolicies = new();
    private readonly Dictionary<ArchetypeId, List<IMappingCreateDeletePolicyFactory>> _archetypeCreateDeletePolicyFactories = new();
    private readonly List<IMappingCreateDeletePolicyFactory> _createDeletePolicyFactories = new();

    private readonly object _dataLock = new();
    private readonly Dictionary<(ArchetypeId, Type, Type), IMappingDataPolicyBase> _dataPolicies = new();
    private readonly Dictionary<ArchetypeId, List<IMappingDataPolicyFactory>> _archetypeDataPolicyFactories = new();
    private readonly List<IMappingDataPolicyFactory> _dataPolicyFactories = new();

    private readonly object _eventLock = new();
    private readonly Dictionary<(Type, Type), IMappingEventPolicyBase> _eventPolicies = new();
    private readonly List<IMappingEventPolicyFactory> _eventPolicyFactories = new();

    public IMappingCreateDeletePolicy<TGameObject> ForCreateDelete<TGameObject>(ArchetypeId archetypeId)
        where TGameObject : class
    {
        lock (_createDeleteLock)
        {
            var key = (archetypeId, typeof(TGameObject));
            if (!_createDeletePolicies.TryGetValue(key, out var untypedPolicy))
            {
                if (_archetypeCreateDeletePolicyFactories.TryGetValue(archetypeId, out var factories))
                {
                    foreach (var factory in factories)
                    {
                        if (!factory.Supports(typeof(TGameObject)))
                            continue;

                        untypedPolicy = factory.CreatePolicy(archetypeId, typeof(TGameObject));
                        break;
                    }
                }

                if (untypedPolicy == null)
                {
                    foreach (var factory in _createDeletePolicyFactories)
                    {
                        if (!factory.Supports(typeof(TGameObject)))
                            continue;

                        untypedPolicy = factory.CreatePolicy(archetypeId, typeof(TGameObject));
                        break;
                    }
                }

                if (untypedPolicy == null)
                    throw new ArgumentException($"No create/delete policy registered for archetype {archetypeId} and game object type {typeof(TGameObject)}");

                _createDeletePolicies.Add(key, untypedPolicy);
            }

            return (IMappingCreateDeletePolicy<TGameObject>)untypedPolicy;
        }
    }

    public IMappingDataPolicy<TContext> ForData<TData, TContext>(ArchetypeId archetypeId)
        where TData : struct, IMappingContext<TContext>
        where TContext : struct
    {
        lock (_dataLock)
        {
            var key = (archetypeId, typeof(TData), typeof(TContext));

            if (!_dataPolicies.TryGetValue(key, out var untypedPolicy))
            {
                if (_archetypeDataPolicyFactories.TryGetValue(archetypeId, out var factories))
                {
                    foreach (var factory in factories)
                    {
                        if (!factory.Supports(typeof(TData), typeof(TContext)))
                            continue;

                        untypedPolicy = factory.CreatePolicy<TContext>(archetypeId, typeof(TData));
                        break;
                    }
                }

                if (untypedPolicy == null)
                {
                    foreach (var factory in _dataPolicyFactories)
                    {
                        if (!factory.Supports(typeof(TData), typeof(TContext)))
                            continue;

                        untypedPolicy = factory.CreatePolicy<TContext>(archetypeId, typeof(TData));
                        break;
                    }
                }

                if (untypedPolicy == null)
                    throw new ArgumentException($"No data policy registered for archetype {archetypeId} and data type {typeof(TData)}");

                _dataPolicies.Add(key, untypedPolicy);
            }

            return (IMappingDataPolicy<TContext>)untypedPolicy;
        }
    }

    public IMappingDataPolicy<Entity> ForData<TData>(ArchetypeId archetypeId)
        where TData : struct, IMappingContext<Entity>
        => ForData<TData, Entity>(archetypeId);

    public IMappingEventPolicy<TContext> ForEvent<TEvent, TContext>()
        where TEvent : struct, IMappingContext<TContext>
        where TContext : struct
    {
        lock (_eventLock)
        {
            var key = (typeof(TEvent), typeof(TContext));

            if (!_eventPolicies.TryGetValue(key, out var untypedPolicy))
            {
                foreach (var factory in _eventPolicyFactories)
                {
                    if (!factory.Supports(typeof(TEvent), typeof(TContext)))
                        continue;

                    untypedPolicy = factory.CreatePolicy<TContext>(typeof(TEvent));
                    break;
                }

                if (untypedPolicy == null)
                    throw new ArgumentException($"No event policy registered for event type {typeof(TEvent)}");

                _eventPolicies.Add(key, untypedPolicy);
            }

            return (IMappingEventPolicy<TContext>)untypedPolicy;
        }
    }

    public IMappingEventPolicy<Entity> ForEvent<TEvent>()
        where TEvent : struct, IMappingContext<Entity>
        => ForEvent<TEvent, Entity>();

    // ---

    public void RegisterDefaultCreateDelete(IMappingCreateDeletePolicyFactory factory)
    {
        _createDeletePolicyFactories.Add(factory);
    }

    public void RegisterDefaultCreateDelete(ArchetypeId archetypeId, IMappingCreateDeletePolicyFactory factory)
    {
        if (!_archetypeCreateDeletePolicyFactories.TryGetValue(archetypeId, out var factories))
        {
            factories = new List<IMappingCreateDeletePolicyFactory>();
            _archetypeCreateDeletePolicyFactories.Add(archetypeId, factories);
        }

        factories.Add(factory);
    }

    public void RegisterDefaultCreateDelete<TGameObject>(
        Func<TGameObject, bool> shouldCreatePropagate,
        Func<Entity, bool> shouldDeletePropagate)
        where TGameObject : class
    {
        var policy = new FuncCreateDeletePolicy<TGameObject>(shouldCreatePropagate, shouldDeletePropagate);
        var factory = new FuncCreateDeletePolicyFactory<TGameObject>(_ => policy);
        RegisterDefaultCreateDelete(factory);
    }

    public void RegisterCreateDelete<TGameObject>(ArchetypeId archetypeId, IMappingCreateDeletePolicy<TGameObject> policy)
        where TGameObject : class
    {
        _createDeletePolicies.Add((archetypeId, typeof(TGameObject)), policy);
    }

    public void RegisterCreateDelete<TGameObject>(
        ArchetypeId archetypeId,
        Func<TGameObject, bool> shouldCreatePropagate,
        Func<Entity, bool> shouldDeletePropagate)
        where TGameObject : class
    {
        var policy = new FuncCreateDeletePolicy<TGameObject>(shouldCreatePropagate, shouldDeletePropagate);
        RegisterCreateDelete(archetypeId, policy);
    }

    // ---

    public void RegisterDefaultData(IMappingDataPolicyFactory factory)
    {
        _dataPolicyFactories.Add(factory);
    }

    public void RegisterDefaultData<TContext>(
        Func<TContext, bool> shouldEcsCopyToGame,
        Func<TContext, bool> shouldGameCopyToEcs,
        Func<TContext, bool> shouldSetLocally)
    {
        var factory = new FuncDataPolicyFactory<TContext>(shouldEcsCopyToGame, shouldGameCopyToEcs, shouldSetLocally);
        RegisterDefaultData(factory);
    }

    public void RegisterDefaultData(ArchetypeId archetypeId, IMappingDataPolicyFactory factory)
    {
        if (!_archetypeDataPolicyFactories.TryGetValue(archetypeId, out var factories))
        {
            factories = new List<IMappingDataPolicyFactory>();
            _archetypeDataPolicyFactories.Add(archetypeId, factories);
        }

        factories.Add(factory);
    }

    public void RegisterDefaultData<TContext>(
        ArchetypeId archetypeId,
        Func<TContext, bool> shouldEcsCopyToGame,
        Func<TContext, bool> shouldGameCopyToEcs,
        Func<TContext, bool> shouldSetLocally)
    {
        var factory = new FuncDataPolicyFactory<TContext>(shouldEcsCopyToGame, shouldGameCopyToEcs, shouldSetLocally);
        RegisterDefaultData(archetypeId, factory);
    }

    public void RegisterData<TData, TContext>(ArchetypeId archetypeId, IMappingDataPolicy<TContext> policy)
        where TData : struct
        where TContext : struct
    {
        _dataPolicies.Add((archetypeId, typeof(TData), typeof(TContext)), policy);
    }

    public void RegisterData<TData>(
        ArchetypeId archetypeId,
        Func<Entity, bool> shouldEcsCopyToGame,
        Func<Entity, bool> shouldGameCopyToEcs,
        Func<Entity, bool> shouldRunLocally)
        where TData : struct
    {
        var policy = new FuncDataPolicy<Entity>(
            shouldEcsCopyToGame,
            shouldGameCopyToEcs,
            shouldRunLocally);
        RegisterData<TData, Entity>(archetypeId, policy);
    }

    // ---

    public void RegisterDefaultEvent(IMappingEventPolicyFactory factory)
    {
        _eventPolicyFactories.Add(factory);
    }

    public void RegisterDefaultEvent<TContext>(
        Func<TContext, bool> shouldGameEventPropagate,
        Func<TContext, bool> shouldEcsEventPropagate,
        ShouldRunLocallyDelegate<TContext> shouldRunLocally)
    {
        var policyFactory = new FuncEntityEventPolicyFactory<TContext>(
            shouldGameEventPropagate,
            shouldEcsEventPropagate,
            shouldRunLocally);
        RegisterDefaultEvent(policyFactory);
    }

    public void RegisterEvent<TEvent, TContext>(IMappingEventPolicy<TContext> policy)
        where TEvent : struct, IEquatable<TEvent>
        where TContext : struct
    {
        _eventPolicies.Add((typeof(TEvent), typeof(TContext)), policy);
    }

    public void RegisterEvent<TEvent, TContext>(
        ShouldPropagateToEcsDelegate<TContext> shouldPropagateToEcs,
        ShouldPropagateToGameDelegate<TContext> shouldPropagateToGame,
        ShouldRunLocallyDelegate<TContext> shouldRunLocally)
        where TEvent : struct, IEquatable<TEvent>
        where TContext : struct
    {
        var policy = new FuncEventPolicy<TEvent, TContext>(shouldPropagateToEcs, shouldPropagateToGame, shouldRunLocally, sideChannel);
        RegisterEvent<TEvent, TContext>(policy);
    }

    public void RegisterEvent<TEvent>(
        ShouldPropagateToEcsDelegate<Entity> shouldPropagateToEcs,
        ShouldPropagateToGameDelegate<Entity> shouldPropagateToGame,
        ShouldRunLocallyDelegate<Entity> shouldRunLocally)
        where TEvent : struct, IEquatable<TEvent>
        => RegisterEvent<TEvent, Entity>(
            shouldPropagateToEcs,
            shouldPropagateToGame,
            shouldRunLocally);
}