using System.Runtime.CompilerServices;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// Represents an entity in the ECS.
/// </summary>
public readonly struct Entity
{
    private readonly GetComponentPointerDelegate _getComponentPointer;
    private readonly ModComponentIds _componentIds;

    internal Entity(int id, GetComponentPointerDelegate getComponentPointer, ModComponentIds componentIds)
    {
        Id = id;
        _getComponentPointer = getComponentPointer;
        _componentIds = componentIds;
    }

    /// <summary>
    /// The identifier of the entity.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets a reference to the component of type T associated with this entity.
    /// </summary>
    /// <typeparam name="T">Type of the component</typeparam>
    /// <returns>A mutable reference to the component.</returns>
    /// <remarks>Attempting to access a component that does not exist on the entity's archetype will crash your mod.</remarks>
    public unsafe ref T GetComponent<T>() where T : struct
    {
        var compId = _componentIds.Resolve<T>();
        var ptr = _getComponentPointer(Id, compId);
        return ref Unsafe.AsRef<T>((void*)ptr);
    }
}