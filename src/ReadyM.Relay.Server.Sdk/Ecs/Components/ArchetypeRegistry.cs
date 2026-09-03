using System.Reflection;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

internal sealed class ArchetypeRegistry : IArchetypeRegistry, IHostedService
{
    private readonly ILogger _logger;

    private readonly RegisterArchetypeDelegate _registerArchetypeDelegate;
    private readonly ModifyArchetypeDelegate _modifyArchetypeDelegate;

    private readonly Dictionary<ArchetypeId, ArchetypeEntry> _archetypeEntries = [];
    private readonly CollectComponentIdsCallback _componentIdCallback;
    private readonly ComponentInitCallback _componentInitCallback;
    private readonly List<IArchetypeBuilderCallback> _filters = [];

    private readonly IEnumerable<IArchetypeRegistration> _registrations;

    public ArchetypeRegistry(ArchetypePointers pointers, IEnumerable<IArchetypeRegistration> registrations, ComponentRegistry registry, EcsApi ecs, ILogger logger)
    {
        _logger = logger;
        _componentIdCallback = new CollectComponentIdsCallback(registry, _logger);
        _componentInitCallback = new ComponentInitCallback(ecs, _logger);
        _registrations = registrations;

        _registerArchetypeDelegate = Marshal.GetDelegateForFunctionPointer<RegisterArchetypeDelegate>(pointers.RegisterArchetype);
        _modifyArchetypeDelegate = Marshal.GetDelegateForFunctionPointer<ModifyArchetypeDelegate>(pointers.ModifyArchetype);
    }

    public void OnScopeStart()
    {
        foreach (var registration in _registrations)
        {
            registration.Register(this);
        }
    }

    private struct ArchetypeEntry
    {
        public ArchetypeBuilder Builder;
        public List<int> ComponentIds;
        public Action<int>? PostCreateInit;
    }

    private sealed class CollectComponentIdsCallback(ComponentRegistry registry, ILogger logger) : IArchetypeBuilderCallback
    {
        public List<int>? ComponentIds;

        public void AcceptComponentType<T>(ArchetypeBuilder builder)
            where T : struct, IComponent
        {
            var componentId = registry.ResolveComponentId<T>();
            if (!ComponentIds!.Contains(componentId))
                ComponentIds.Add(componentId);
        }

        public void AcceptComponentType<T>(ArchetypeBuilder builder, T defaultValue) where T : struct, IComponent
            => AcceptComponentType<T>(builder);

        public void AcceptStrideComponent(ArchetypeBuilder builder, int structIndex, int stride)
            => throw new NotSupportedException("Adding components by struct index is not supported in the mod archetype registry.");

        public void AcceptTag<T>(ArchetypeBuilder builder)
            where T : struct, ITag
            => throw new NotSupportedException("Adding tag components is not supported in the mod archetype registry.");
    }

    private class ComponentInitCallback(EcsApi ecs, ILogger logger) : IArchetypeBuilderCallback
    {
        public Action<int>? PostCreateInit;

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

                PostCreateInit = (Action<int>?)Delegate.Combine(PostCreateInit, new Action<int>(entityId =>
                {
                    ref var comp = ref ecs.GetComponentRef<T>(entityId);
                    del.Invoke(ref comp, AllocatorKind.Default);
                }));
            }
        }

        public void AcceptComponentType<T>(ArchetypeBuilder builder, T defaultValue)
            where T : struct, IComponent
        {
            // Before the native init, which allocates into the struct and would be overwritten by this assignment.
            PostCreateInit = (Action<int>?)Delegate.Combine(PostCreateInit, new Action<int>(entityId =>
            {
                ecs.GetComponentRef<T>(entityId) = defaultValue;
            }));

            AcceptComponentType<T>(builder);
        }

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

    /// <summary>
    /// Collects the default-value and native-init handlers for everything currently on the builder. Accept replays
    /// the builder's components once, so the callback has to be reset around it or handlers leak into the next
    /// archetype.
    /// </summary>
    private Action<int>? CreatePostCreateInit(ArchetypeBuilder builder)
    {
        try
        {
            _componentInitCallback.PostCreateInit = null;
            builder.Accept(_componentInitCallback);
            return _componentInitCallback.PostCreateInit;
        }
        finally
        {
            _componentInitCallback.PostCreateInit = null;
        }
    }

    private List<int> GetComponentIds(int startIndex, ArchetypeBuilder builder)
    {
        var componentIds = new List<int>();
        _componentIdCallback.ComponentIds = componentIds;
        builder.Accept(_componentIdCallback);
        _componentIdCallback.ComponentIds = null;

        return componentIds;
    }

    private NativeList<int> ToNative(List<int> lst)
    {
        var componentList = new NativeList<int>(lst.Count, AllocatorKind.Default);
        foreach (var id in lst)
        {
            componentList.Add(id);
        }

        return componentList;
    }

    public ArchetypeId RegisterArchetype(ArchetypeBuilder builder)
    {
        foreach (var filter in _filters)
        {
            builder.RegisterFilter(filter);
        }

        var componentList = GetComponentIds(0, builder);
        var nativeComponentList = ToNative(componentList);
        var archetypeId = _registerArchetypeDelegate(nativeComponentList);

        _archetypeEntries[archetypeId] = new ArchetypeEntry
        {
            Builder = builder,
            ComponentIds = componentList,
            PostCreateInit = CreatePostCreateInit(builder),
        };

        _logger.LogDebug("Registering archetype {Archetype} {Components}", archetypeId, componentList);

        return archetypeId;
    }

    public void ModifyArchetype(ArchetypeId archetypeId, Action<ArchetypeBuilder> callback)
    {
        if (!_archetypeEntries.TryGetValue(archetypeId, out var entry))
        {
            // NOTE: We're optimistically assuming that the corresponding archetype exists on the native server side.
            // This should work normally, as we're passing only the newly added components, not all.
            entry = new ArchetypeEntry
            {
                Builder = new ArchetypeBuilder(),
                ComponentIds = [],
            };
            _archetypeEntries[archetypeId] = entry;
        }

        var startIndex = entry.ComponentIds.Count;
        callback(entry.Builder);

        var newComponentList = GetComponentIds(startIndex, entry.Builder);
        entry.ComponentIds.AddRange(newComponentList);

        // The builder only holds what this mod put on the archetype, which is exactly what we are responsible for.
        entry.PostCreateInit = CreatePostCreateInit(entry.Builder);
        _archetypeEntries[archetypeId] = entry;

        _logger.LogDebug("Modifying archetype {Archetype} {Components}", archetypeId, newComponentList);

        var nativeNewComponentList = ToNative(newComponentList);
        _modifyArchetypeDelegate(archetypeId, nativeNewComponentList);
    }
    
    public void RunPostCreateInit(ArchetypeId archetypeId, int entityId)
    {
        if (!_archetypeEntries.TryGetValue(archetypeId, out var entry) || entry.PostCreateInit == null)
            return;

        try
        {
            entry.PostCreateInit.Invoke(entityId);
        }
        catch (Exception e)
        {
            // Throwing here would propagate across the interop border out of the host's entity creation.
            _logger.LogError(e, "Native init failed for entity {EntityId} of archetype {Archetype}", entityId, archetypeId);
        }
    }

    public void RegisterFilter(IArchetypeBuilderCallback filter)
    {
        _filters.Add(filter);

        foreach (var entry in _archetypeEntries.Values)
        {
            entry.Builder.RegisterFilter(filter);
        }
    }

    public void Dispose()
    {
        // do nothing
    }
}