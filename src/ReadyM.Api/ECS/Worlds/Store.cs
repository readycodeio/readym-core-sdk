using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Generators;
using ReadyM.Api.Idents;

namespace ReadyM.Api.ECS.Worlds;

[WrapperFor(typeof(EntityStore))]
[WrapperInclude("^Query.*")]
[WrapperInclude("^Count$")]
[WrapperInclude("^GetCommandBuffer$")] // TODO: Wrap to disable entity creation
[WrapperInclude("^OnEntit.*")] // TODO: Events expose underlying EntityStore
[WrapperInclude("^OnTag.*")]
[WrapperInclude("^EventRecorder")]
public sealed partial class Store
{
    private byte _nextArchetypeId;
    private readonly Dictionary<ArchetypeId, Action<EntityBuilder>> _archetypeConstructors = [];

    public SystemRoot SystemRoot { get; }

    public Store(EntityStore wrapped)
    {
        _wrapped = wrapped;
        SystemRoot = new SystemRoot();
        SystemRoot.AddStore(wrapped);
#if DEBUG
        SystemRoot.SetMonitorPerf(true);
#endif
    }

    public ArchetypeId RegisterArchetype(Action<EntityBuilder> populateComponents)
    {
        var id = _nextArchetypeId++;
        var archetypeId = new ArchetypeId(id);
        _archetypeConstructors[archetypeId] = populateComponents;
        return archetypeId;
    }

    public Entity CreateEntity(ArchetypeId archetypeId, Action<EntityBuilder>? setComponents = null)
    {
        if (!_archetypeConstructors.TryGetValue(archetypeId, out var constructor))
        {
            throw new ArgumentException($"Archetype with ID {archetypeId} is not registered.");
        }

        var batch = _wrapped.Batch();
        var builder = new EntityBuilder(batch);
        constructor!.Invoke(builder);
        setComponents?.Invoke(builder);
        return batch.CreateEntity();
    }
    
    internal Entity CreateEntity(Action<EntityBuilder>? setComponents = null)
    {
        var batch = _wrapped.Batch();
        var builder = new EntityBuilder(batch);
        setComponents?.Invoke(builder);
        return batch.CreateEntity();
    }

    /// <summary>
    /// Returns the index for indexed components to search entities with a specific component value in O(1).<br/>
    /// Executes in O(1). 
    /// </summary>
    public ComponentIndex<TIndexedComponent, TValue> ComponentIndex<TIndexedComponent, TValue>()
        where TIndexedComponent : struct, IIndexedComponent<TValue>
    {
        return _wrapped.ComponentIndex<TIndexedComponent, TValue>();
    }

    /// <summary>
    /// Returns the index for link components to search entities with a specific entity in O(1).<br/>
    /// Executes in O(1). 
    /// </summary>
    public LinkComponentIndex<TLinkComponent> LinkComponentIndex<TLinkComponent>()
        where TLinkComponent : struct, ILinkComponent
    {
        return _wrapped.LinkComponentIndex<TLinkComponent>();
    }
}