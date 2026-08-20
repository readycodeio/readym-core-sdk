using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

internal sealed class ArchetypeRegistry(ArchetypePointers pointers, ServerSideSettings serverSide, ComponentRegistry registry, ILogger logger) : IArchetypeRegistry
{
    private struct ArchetypeEntry
    {
        public ArchetypeBuilder Builder;
        public List<int> ComponentIds;
    }

    private sealed class CollectComponentIdsCallback(ComponentRegistry registry, ServerSideSettings serverSide, ILogger logger) : IArchetypeBuilderCallback
    {
        public List<int>? ComponentIds;

        public void AcceptComponentType<T>(ArchetypeBuilder builder)
            where T : struct, IComponent
        {
            var componentId = registry.ResolveComponentId(typeof(T));
            if (!ComponentIds!.Contains(componentId))
                ComponentIds.Add(componentId);
        }

        public void AcceptComponentType<T>(ArchetypeBuilder builder, T defaultValue) where T : struct, IComponent
        {
            logger.LogError("Default value {DefaultValue} for type {ComponentType} are not set when adding a " +
                            "component with a value. Use Add<T>() and set the values manually.", defaultValue, typeof(T).Name);

            AcceptComponentType<T>(builder);
        }

        public void AcceptStrideComponent(ArchetypeBuilder builder, int structIndex, int stride)
            => throw new NotSupportedException("Adding components by struct index is not supported in the mod archetype registry.");

        public void AcceptTag<T>(ArchetypeBuilder builder)
            where T : struct, ITag
            => throw new NotSupportedException("Adding tag components is not supported in the mod archetype registry.");
    }

    private readonly RegisterArchetypeDelegate _registerArchetypeDelegate =
        Marshal.GetDelegateForFunctionPointer<RegisterArchetypeDelegate>(pointers.RegisterArchetype);

    private readonly ModifyArchetypeDelegate _modifyArchetypeDelegate =
        Marshal.GetDelegateForFunctionPointer<ModifyArchetypeDelegate>(pointers.ModifyArchetype);

    private readonly Dictionary<ArchetypeId, ArchetypeEntry> _archetypeEntries = [];
    private readonly CollectComponentIdsCallback _callback = new(registry, serverSide, logger);

    private List<int> GetComponentIds(int startIndex, ArchetypeBuilder builder)
    {
        var componentIds = new List<int>();
        _callback.ComponentIds = componentIds;
        builder.Accept(_callback);
        _callback.ComponentIds = null;
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
        var componentList = GetComponentIds(0, builder);
        var nativeComponentList = ToNative(componentList);
        var archetypeId = _registerArchetypeDelegate(nativeComponentList);

        _archetypeEntries[archetypeId] = new ArchetypeEntry
        {
            Builder = builder,
            ComponentIds = componentList,
        };

        logger.LogDebug("Registering archetype {Archetype} {Components}", archetypeId, componentList);

        return archetypeId;
    }

    public void ModifyArchetype(ArchetypeId archetypeId, Action<ArchetypeBuilder> callback)
    {
        if (!_archetypeEntries.TryGetValue(archetypeId, out var entry))
        {
            throw new ArgumentException($"Archetype with ID {archetypeId} is not registered.");
        }

        var startIndex = entry.ComponentIds.Count;
        callback(entry.Builder);
        var newComponentList = GetComponentIds(startIndex, entry.Builder);
        entry.ComponentIds.AddRange(newComponentList);

        logger.LogDebug("Modifying archetype {Archetype} {Components}", archetypeId, newComponentList);

        var nativeNewComponentList = ToNative(newComponentList);
        _modifyArchetypeDelegate(archetypeId, nativeNewComponentList);
    }
}
