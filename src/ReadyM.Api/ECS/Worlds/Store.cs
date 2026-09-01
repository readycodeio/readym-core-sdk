using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Generators;
using ReadyM.Api.Idents;
using Yooni.Native.LowLevel;

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
        public Action<Entity>? PostCreateInit;
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

    private class NativeInitCallback(ILogger logger) : IArchetypeBuilderCallback
    {
        public Action<Entity>? PostCreateInit;

        delegate void NativeInitDelegate<T>(ref T comp, AllocatorKind allocatorKind);

        public void AcceptComponentType<T>(ArchetypeBuilder builder)
            where T : struct, IComponent
        {
            if (typeof(INativeInit).IsAssignableFrom(typeof(T)))
            {
                var method = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .SingleOrDefault(m => m.Name == nameof(INativeInit.Init) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(AllocatorKind));

                if (method == null)
                {
                    logger.LogWarning("Component type {ComponentType} implements INativeInit but does not have a valid Init method. Skipping native init.", typeof(T));
                    return;
                }

                var del = (NativeInitDelegate<T>)method.CreateDelegate(typeof(NativeInitDelegate<T>));

                PostCreateInit = (Action<Entity>?)Delegate.Combine(PostCreateInit, new Action<Entity>(e =>
                {
                    ref var comp = ref e.GetComponent<T>();
                    del.Invoke(ref comp, AllocatorKind.Default);
                }));
            }
        }

        public void AcceptComponentType<T>(ArchetypeBuilder builder, T defaultValue)
            where T : struct, IComponent
            => AcceptComponentType<T>(builder);

        public void AcceptStrideComponent(ArchetypeBuilder builder, int structIndex, int stride)
        {
            // empty
        }

        public void AcceptTag<T>(ArchetypeBuilder builder)
            where T : struct, ITag
        {
            // empty
        }
    }

    private readonly ILogger _logger;

    /// <summary>
    /// Runs native init for components the mod host owns. Those are registered here as opaque stride components, so
    /// <see cref="NativeInitCallback"/> cannot see them and the mod side has to do it. Null when no mod host is
    /// attached, which is always the case on the client.
    /// </summary>
    private Action<ArchetypeId, int>? _modPostCreateInit;

    private Thread? _thread;
    private byte _nextArchetypeId;
    private readonly Dictionary<ArchetypeId, ArchetypeEntry> _archetypeEntries = [];
    private readonly CreateEntityBatchCallback _consCallback;
    private readonly NativeInitCallback _nativeInitCallback;
    private readonly List<IArchetypeBuilderCallback> _filters = [];

    public SystemRoot SystemRoot { get; }

    // TODO: the ArchetypeId on client and server are only in sync because the order of registration is the same
    // This is fragile and should be fixed. It's only a coincidence that the DI injection order is the same.
    public Store(EntityStore wrapped, ILogger logger, IEnumerable<IArchetypeRegistration> registrations)
    {
        _wrapped = wrapped;
        _logger = logger;
        _consCallback = new CreateEntityBatchCallback();
        _nativeInitCallback = new NativeInitCallback(logger);

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
                _consCallback.Batch = b;
                builder.Accept(_consCallback);
            }
            finally
            {
                _consCallback.Batch = null;
            }
        };
    }

    private Action<Entity>? CreatePostCreateInit(ArchetypeBuilder builder)
    {
        try
        {
            _nativeInitCallback.PostCreateInit = null;
            builder.Accept(_nativeInitCallback);
            var result = _nativeInitCallback.PostCreateInit;
            return result;
        }
        finally
        {
            _nativeInitCallback.PostCreateInit = null;
        }
    }

    public ArchetypeId RegisterArchetype(ArchetypeBuilder builder)
    {
        var id = _nextArchetypeId++;
        var archetypeId = new ArchetypeId(id);
        var cons = CreateConstructor(builder);
        var postCreateInit = CreatePostCreateInit(builder);

        foreach (var filter in _filters)
        {
            builder.RegisterFilter(filter);
        }

        _archetypeEntries[archetypeId] = new ArchetypeEntry
        {
            Builder = builder,
            Constructor = cons,
            PostCreateInit = postCreateInit,
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
        entry.PostCreateInit = CreatePostCreateInit(entry.Builder);
        _archetypeEntries[archetypeId] = entry;
    }

    /// <summary>
    /// Registers the mod host's native init hook. Called once during mod host initialisation.
    /// </summary>
    public void SetModPostCreateInit(Action<ArchetypeId, int>? callback)
        => _modPostCreateInit = callback;

    internal Entity CreateEntity(ArchetypeId archetypeId, Action<EntityBuilder>? setComponents = null)
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
        entry.PostCreateInit?.Invoke(entity);

        // Mod components are stride components here, so their init has to happen on the mod side. This runs before
        // anything can observe the entity, which matters for remote entities: the containers must exist before a
        // snapshot or delta is applied into them.
        _modPostCreateInit?.Invoke(archetypeId, entity.Id);

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

    public void RegisterFilter(IArchetypeBuilderCallback filter)
    {
        _filters.Add(filter);

        foreach (var entry in _archetypeEntries.Values)
        {
            entry.Builder.RegisterFilter(filter);
        }
    }

    internal void ForceAOT<T>()
        where T : struct, IComponent
    {
        default(NativeInitCallback)!.AcceptComponentType<T>(null!);
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (default(T) is INativeInit nativeInit)
            nativeInit.Init(default);
    }
}