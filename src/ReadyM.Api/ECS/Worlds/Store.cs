using System;
using System.Collections.Generic;
using System.Threading;
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
[WrapperInclude("^GetEntityBy.*")]
[WrapperInclude("^TryGetEntityById$")]
[WrapperInclude("^OnEntit.*")] // TODO: Events expose underlying EntityStore
[WrapperInclude("^OnTag.*")]
[WrapperInclude("^EventRecorder")]
[WrapperInclude("^GetEntity.*")]
internal sealed partial class Store : IArchetypeRegistry
{
    private struct ArchetypeEntry
    {
        public Action<EntityBuilder> Constructor;
    }

    private Thread? _thread;
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

        OnEntityDelete += _ => { AssertThreadId(); };
    }

    public void SetThread(Thread newThread)
    {
        _thread = newThread;
    }

    private void AssertThreadId()
    {
        if (Thread.CurrentThread != _thread)
        {
            throw new InvalidOperationException("Store can only be accessed from the thread it was created on.");
        }
    }

    public ArchetypeId RegisterArchetype(Action<EntityBuilderBase> constructor)
    {
        var id = _nextArchetypeId++;
        var archetypeId = new ArchetypeId(id);
        _archetypeEntries[archetypeId] = new ArchetypeEntry
        {
            Constructor = constructor
        };
        return archetypeId;
    }

    public void ModifyArchetype(ArchetypeId archetypeId, Action<EntityBuilderBase> constructor)
    {
        if (!_archetypeEntries.TryGetValue(archetypeId, out var entry))
        {
            throw new ArgumentException($"Archetype with ID {archetypeId} is not registered.");
        }

        _archetypeEntries[archetypeId] = new ArchetypeEntry
        {
            Constructor = builder =>
            {
                entry.Constructor(builder);
                constructor(builder);
            }
        };
    }

    internal Entity CreateEntity(ArchetypeId archetypeId, Action<EntityBuilder>? setComponents = null)
    {
        AssertThreadId();

        if (!_archetypeEntries.TryGetValue(archetypeId, out var entry))
        {
            throw new ArgumentException($"Archetype with ID {archetypeId} is not registered.");
        }

        var batch = _wrapped.Batch();
        var builder = new EntityBuilder(batch);
        entry.Constructor.Invoke(builder);
        setComponents?.Invoke(builder);
        var entity = batch.CreateEntity();
        return entity;
    }

    internal Entity CreateEntity(Action<EntityBuilder>? setComponents = null)
    {
        AssertThreadId();

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