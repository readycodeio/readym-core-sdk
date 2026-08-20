using System;
using System.Collections.Generic;
using System.Threading;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
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
        public ArchetypeBuilder Builder;
        public Action<CreateEntityBatch> Constructor;
    }

    private class CreateEntityBatchCallback : IArchetypeBuilderCallback
    {
        public CreateEntityBatch? Batch;

        public void AcceptComponentType<T>(ArchetypeBuilder builder)
            where T : struct, IComponent
            => Batch!.Add<T>();

        public void AcceptComponentType<T>(ArchetypeBuilder builder, T defaultValue)
            where T : struct, IComponent
            => Batch!.Add<T>(defaultValue);

        public void AcceptStrideComponent(ArchetypeBuilder builder, int structIndex, int stride)
            => Batch!.Add(structIndex, stride);

        public void AcceptTag<T>(ArchetypeBuilder builder)
            where T : struct, ITag
            => Batch!.AddTag<T>();
    }

    private readonly ILogger _logger;

    private Thread? _thread;
    private byte _nextArchetypeId;
    private readonly Dictionary<ArchetypeId, ArchetypeEntry> _archetypeEntries = [];
    private readonly CreateEntityBatchCallback _callback = new();

    public SystemRoot SystemRoot { get; }

    // TODO: the ArchetypeId on client and server are only in sync because the order of registration is the same
    // This is fragile and should be fixed. It's only a coincidence that the DI injection order is the same.
    public Store(EntityStore wrapped, ILogger logger, IEnumerable<IArchetypeRegistration> registrations)
    {
        _wrapped = wrapped;
        _logger = logger;

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

    private Action<CreateEntityBatch> CreateConstructor(ArchetypeBuilder builder)
    {
        return b =>
        {
            try
            {
                _callback.Batch = b;
                builder.Accept(_callback);
            }
            finally
            {
                _callback.Batch = null;
            }
        };
    }

    public ArchetypeId RegisterArchetype(ArchetypeBuilder builder)
    {
        var id = _nextArchetypeId++;
        var archetypeId = new ArchetypeId(id);
        var cons = CreateConstructor(builder);

        _archetypeEntries[archetypeId] = new ArchetypeEntry
        {
            Builder = builder,
            Constructor = cons,
        };

        _logger.LogDebug("Registering archetype {ArchetypeId} {Builder}", archetypeId, builder);

        return archetypeId;
    }

    public void ModifyArchetype(ArchetypeId archetypeId, Action<ArchetypeBuilder> callback)
    {
        if (!_archetypeEntries.TryGetValue(archetypeId, out var entry))
        {
            throw new ArgumentException($"Archetype with ID {archetypeId} is not registered.");
        }

        _logger.LogDebug("Modifying archetype {ArchetypeId} {Target}:{Method}", archetypeId, entry.Builder, callback.Method);

        callback(entry.Builder);
        entry.Constructor = CreateConstructor(entry.Builder);
        _archetypeEntries[archetypeId] = entry;
    }

    internal Entity CreateEntity(ArchetypeId archetypeId, Action<EntityBuilderBase>? setComponents = null)
    {
        AssertThreadId();

        if (!_archetypeEntries.TryGetValue(archetypeId, out var entry))
        {
            throw new ArgumentException($"Archetype with ID {archetypeId} is not registered.");
        }

        var batch = _wrapped.Batch();
        var builder = new EntityBuilder(batch);
        entry.Constructor.Invoke(batch);
        setComponents?.Invoke(builder);
        var entity = batch.CreateEntity();
        return entity;
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
