using System.Runtime.CompilerServices;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// Represents an entity in the ECS.
/// </summary>
public readonly struct Entity
{
    private readonly int _id;
    private readonly GetComponentPointerDelegate _getComponentPointer;
    private readonly ComponentRegistry _registry;

    internal Entity(int id, GetComponentPointerDelegate getComponentPointer, ComponentRegistry registry)
    {
        _id = id;
        _getComponentPointer = getComponentPointer;
        _registry = registry;
    }

    public int Id => _id;

    /// <summary>
    /// Gets a reference to the component of type T associated with this entity.
    /// </summary>
    /// <typeparam name="T">Type of the component</typeparam>
    /// <returns>A mutable reference to the component.</returns>
    /// <remarks>Attempting to access a component that does not exist on the entity's archetype will crash your mod.</remarks>
    public unsafe ref T GetComponent<T>() where T : struct
    {
        var compId = _registry.ResolveComponentId<T>();
        var ptr = _getComponentPointer(_id, compId);
        return ref Unsafe.AsRef<T>((void*)ptr);
    }
}