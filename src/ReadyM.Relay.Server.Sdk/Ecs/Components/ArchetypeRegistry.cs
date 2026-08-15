using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

internal sealed class ArchetypeRegistry(ArchetypePointers pointers, ComponentRegistry registry, ILogger logger) : IArchetypeRegistry
{
    private readonly RegisterArchetypeDelegate _registerArchetypeDelegate =
        Marshal.GetDelegateForFunctionPointer<RegisterArchetypeDelegate>(pointers.RegisterArchetype);

    private readonly ModifyArchetypeDelegate _modifyArchetypeDelegate =
        Marshal.GetDelegateForFunctionPointer<ModifyArchetypeDelegate>(pointers.ModifyArchetype);


    private sealed class InteropEntityBuilder(ComponentRegistry registry) : EntityBuilderBase
    {
        public HashSet<int> ComponentIds { get; } = [];

        public override EntityBuilderBase Add<T>()
        {
            var componentId = registry.ResolveComponentId<T>();
            ComponentIds.Add(componentId);
            return this;
        }

        // TODO: Default values not set
        [Obsolete("Default values are not set when adding a component with a value. Use Add<T>() and set the values manually.")]
        public override EntityBuilderBase Add<T>(in T component)
        {
            var componentId = registry.ResolveComponentId<T>();
            ComponentIds.Add(componentId);
            return this;
        }
    }

    public ArchetypeId RegisterArchetype(Action<EntityBuilderBase> build)
    {
        var builder = new InteropEntityBuilder(registry);
        build(builder);
        var componentList = new NativeList<int>(builder.ComponentIds.Count, AllocatorKind.Default);
        foreach (var componentId in builder.ComponentIds)
        {
            componentList.Add(componentId);
        }

        var archetypeId = _registerArchetypeDelegate(componentList);

        logger.LogDebug("Registering archetype {Archetype} {Target}:{Method} {Components}", archetypeId, build.Target, build.Method, componentList);

        return archetypeId;
    }

    public void ModifyArchetype(ArchetypeId archetypeId, Action<EntityBuilderBase> build)
    {
        var builder = new InteropEntityBuilder(registry);
        build(builder);
        var componentList = new NativeList<int>(builder.ComponentIds.Count, AllocatorKind.Default);
        foreach (var componentId in builder.ComponentIds)
        {
            componentList.Add(componentId);
        }

        logger.LogDebug("Modifying archetype {Archetype} {Target}:{Method} {Components}", archetypeId, build.Target, build.Method, componentList);

        _modifyArchetypeDelegate(archetypeId, componentList);
    }
}
