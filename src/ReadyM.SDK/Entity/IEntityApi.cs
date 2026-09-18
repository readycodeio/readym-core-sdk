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

    /// <summary>
    /// Writes the whole component and moves it in the index. A write through the reference Locate
    /// hands back cannot do that, so an indexed value has to come this way.
    /// </summary>
    void ReplaceIndexed<TComponent, TKey>(RawEntity rawEntity, TComponent component)
        where TComponent : struct, IIndexedComponent<TKey>;

    /// <summary>
    /// Every entity held by the scope that carries the whole component set.
    /// </summary>
    /// <remarks>
    /// Asked of the scope rather than of the world: the entities in one are found through the link
    /// they already carry, so this costs what the scope holds rather than what the world does.
    /// </remarks>
    EntityBuffer CollectInScope(RawEntity scope, ComponentSet components);

    /// <summary>The entity whose indexed component holds this value, if one does.</summary>
    bool TryFindByIndex<TComponent, TKey>(TKey key, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey>;

    bool IsAlive(RawEntity rawEntity);

    /// Whether the entity carries every component of the set. An empty set matches anything.
    bool HasComponents(RawEntity rawEntity, ComponentSet components);

    /// <exception cref="StructuralChangeInQueryException">A query is running.</exception>
    RawEntity Create(ComponentSet components);

    /// <summary>The same, held by a scope, so the entity goes when the scope does.</summary>
    RawEntity Create(ComponentSet components, RawEntity scope);

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