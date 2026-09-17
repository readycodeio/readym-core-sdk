using ReadyM.SDK.Exceptions;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entity;

internal interface IEntityApi
{
    /// What this side calls the component type. Asked once per type, see <see cref="ComponentIds{T}"/>.
    int ComponentIdOf(Type type);

    /// Where the component lives, or a default that is not <c>Found</c> if it is absent.
    /// <exception cref="InvalidEntityException">The entity is gone, or a running query asked for it to be.</exception>
    ComponentRef Locate(RawEntity rawEntity, int componentId);

    void AddComponent<T>(RawEntity entity) where T : struct, IComponent;
    bool IsAlive(RawEntity rawEntity);

    /// <summary>Whether the entity carries every component of the set. An empty set matches anything.</summary>
    bool HasComponents(RawEntity rawEntity, ComponentSet components);

    /// <exception cref="StructuralChangeInQueryException">A query is running.</exception>
    RawEntity Create(ComponentSet components);

    /// <summary>
    /// Removes the entity, or, inside a query, records that it is to be removed once the loop ends.
    /// False when it was already gone or already asked for.
    /// </summary>
    bool Delete(RawEntity rawEntity);

    /// Called by a query as it starts, so deletes inside can be deferred.
    void EnterQuery();

    /// Called by a query as it ends, so deletes deferred inside can be applied.
    void LeaveQuery();
}
