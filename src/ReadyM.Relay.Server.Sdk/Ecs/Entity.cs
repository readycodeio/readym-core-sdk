using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// Represents an entity in the ECS.
/// </summary>
public readonly struct Entity
{
    private readonly GetComponentSlotDelegate _getComponentSlot;
    private readonly ComponentRegistry _registry;

    internal Entity(int id, GetComponentSlotDelegate getComponentSlot, ComponentRegistry registry)
    {
        Id = id;
        _getComponentSlot = getComponentSlot;
        _registry = registry;
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
        ComponentSlot slot;
        _getComponentSlot(Id, _registry.ResolveComponentId<T>(), &slot);
        return ref EcsApi.SlotRef<T>(slot);
    }
}
