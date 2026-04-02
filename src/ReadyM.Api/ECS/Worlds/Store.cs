using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.ECS.Registry;
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
[WrapperInclude("^GetEntity.*")]
internal sealed partial class Store
{
    private struct ArchetypeEntry
    {
        public Action<EntityBuilder> Constructor;
        public Action<Entity>? LateInit;
    }

    private byte _nextArchetypeId;
    private readonly Dictionary<ArchetypeId, ArchetypeEntry> _archetypeEntries = [];

    public SystemRoot SystemRoot { get; }

    // TODO: the ArchetypeId on client and server are only in sync because the order of registration is the same
    // This is fragile and should be fixed. It's only a coincidence that the DI injection order is the same.
    public Store(EntityStore wrapped, IEnumerable<IArchetypeRegistration> registrations)
    {
        _wrapped = wrapped;
        SystemRoot = new SystemRoot();
        SystemRoot.AddStore(wrapped);
#if DEBUG
        SystemRoot.SetMonitorPerf(true);
#endif

        foreach (var registration in registrations)
        {
            registration.Register(this);
        }
    }

    internal ArchetypeId RegisterArchetype(Action<EntityBuilder> constructor, Action<Entity>? lateInit = null)
    {
        var id = _nextArchetypeId++;
        var archetypeId = new ArchetypeId(id);
        _archetypeEntries[archetypeId] = new ArchetypeEntry
        {
            Constructor = constructor,
            LateInit = lateInit
        };
        return archetypeId;
    }

    internal Entity CreateEntity(ArchetypeId archetypeId, Action<EntityBuilder>? setComponents = null)
    {
        if (!_archetypeEntries.TryGetValue(archetypeId, out var entry))
        {
            throw new ArgumentException($"Archetype with ID {archetypeId} is not registered.");
        }

        var batch = _wrapped.Batch();
        var builder = new EntityBuilder(batch);
        entry.Constructor.Invoke(builder);
        setComponents?.Invoke(builder);
        var entity = batch.CreateEntity();
        entry.LateInit?.Invoke(entity);
        return entity;
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