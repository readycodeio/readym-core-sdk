using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// Represents an entity in the ECS.
/// </summary>
public readonly struct Entity
{
    private readonly RawEntity _rawEntity;
    private readonly GetComponentSlotDelegate _getComponentSlot;
    private readonly ComponentRegistry _registry;

    internal Entity(RawEntity rawEntity, GetComponentSlotDelegate getComponentSlot, ComponentRegistry registry)
    {
        _rawEntity = rawEntity;
        _getComponentSlot = getComponentSlot;
        _registry = registry;
    }

    /// <summary>
    /// The identifier of the entity.
    /// </summary>
    public int Id => _rawEntity.Id;

    /// <summary>
    /// Identity including the revision, which is what tells this entity apart from a later one that took its id.
    /// </summary>
    internal RawEntity RawEntity => _rawEntity;

    /// <summary>
    /// Gets a reference to the component of type T associated with this entity.
    /// </summary>
    /// <typeparam name="T">Type of the component</typeparam>
    /// <returns>A mutable reference to the component.</returns>
    /// <exception cref="InvalidOperationException">
    /// The entity is gone, its id now belongs to another entity, or its archetype does not carry the component.
    /// </exception>
    public unsafe ref T GetComponent<T>() where T : struct
    {
        ComponentSlot slot;
        _getComponentSlot(_rawEntity, 1, _registry.ResolveComponentId<T>(), &slot);

        if (!slot.Found())
            throw new InvalidOperationException($"Entity {_rawEntity.Id} is gone or does not carry {typeof(T).Name}.");

        return ref EcsApi.SlotRef<T>(slot);
    }
}
