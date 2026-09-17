using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Exceptions;

namespace ReadyM.SDK.Client.Entity;

internal sealed class ClientEntityApi : IEntityApi
{
    private readonly EntityStore _store;

    public ClientEntityApi(EntityStore store) => _store = store;

    public int ComponentIdOf(Type type) => SchemaTypeUtils.GetStructIndex(type);

    /// <summary>
    /// One node load rather than the three that going through <see cref="Friflo.Engine.ECS.Entity"/>
    /// cost: resolving the entity, checking it, and reaching the component each loaded it again.
    /// </summary>
    public ComponentRef Locate(RawEntity rawEntity, int componentId)
    {
        var nodes = _store.nodes;

        if ((uint)rawEntity.Id >= (uint)nodes.Length)
            throw new InvalidEntityException();

        ref var node = ref nodes[rawEntity.Id];

        if (!node.IsAlive(rawEntity.Revision))
            throw new InvalidEntityException();

        var heap = node.archetype.heapMap[componentId];

        return heap is null ? default : new ComponentRef(heap.ComponentArray!, node.compIndex);
    }

    public void AddComponent<T>(RawEntity rawEntity) where T : struct, IComponent
        => Resolve(rawEntity).AddComponent<T>();

    public bool HasComponents(RawEntity rawEntity, ComponentSet components)
        => Resolve(rawEntity).Archetype.ComponentTypes.HasAll(ClientComponents.Resolve(components));

    public bool IsAlive(RawEntity rawEntity) => !_store.GetEntityByRawEntity(rawEntity).IsNull;

    private Friflo.Engine.ECS.Entity Resolve(RawEntity rawEntity)
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);

        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity;
    }
}
