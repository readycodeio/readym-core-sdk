using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Exceptions;

namespace ReadyM.SDK.Client.Entities;

internal sealed class ClientEntityApi : IEntityApi
{
    private readonly EntityStore _store;
    private QueryScope _scope = new();

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

        // Kept apart from the bounds check above, which has to stand alone for the JIT to drop the
        // array's own check on the line between them.
        if (!node.IsAlive(rawEntity.Revision) || _scope.IsPending(rawEntity))
            throw new InvalidEntityException();

        var heap = node.archetype.heapMap[componentId];

        return heap is null ? default : new ComponentRef(heap.ComponentArray!, node.compIndex);
    }

    public void AddComponent<T>(RawEntity rawEntity) where T : struct, IComponent
    {
        _scope.RefuseIfInQuery($"Adding {typeof(T).Name}");

        Resolve(rawEntity).AddComponent<T>();
    }

    public RawEntity Create(ComponentSet components, RawEntity scope)
    {
        var holder = Resolve(scope);
        var entity = _store.GetEntityByRawEntity(Create(components));

        entity.AddComponent(new InScopeComponent(holder));

        return entity.RawEntity;
    }

    public RawEntity Create(ComponentSet components)
    {
        _scope.RefuseIfInQuery("Creating an entity");

        return _store.GetArchetype(ClientComponents.Resolve(components)).CreateEntity().RawEntity;
    }

    /// Assigning the whole component is what makes Friflo move it in the index.
    public void ReplaceIndexed<TComponent, TKey>(RawEntity rawEntity, TComponent component)
        where TComponent : struct, IIndexedComponent<TKey>
        => Resolve(rawEntity).AddComponent(component);

    /// Friflo keeps the index, so this is its own lookup.
    public bool TryFindByIndex<TComponent, TKey>(TKey key, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey>
    {
        foreach (var found in _store.ComponentIndex<TComponent, TKey>()[key])
        {
            entity = found.RawEntity;
            return true;
        }

        entity = default;
        return false;
    }

    public EntityBuffer CollectInScope(RawEntity scope, ComponentSet components)
    {
        var target = Resolve(scope);
        var wanted = ClientComponents.Resolve(components);
        var links = target.GetIncomingLinks<InScopeComponent>();
        var buffer = EntityBuffer.Rent();

        for (var i = 0; i < links.Count; i++)
        {
            var entity = links.Entities[i];

            if (entity.Archetype.ComponentTypes.HasAll(wanted))
                buffer.Append(entity.RawEntity);
        }

        return buffer;
    }

    public bool HasComponents(RawEntity rawEntity, ComponentSet components)
        => Resolve(rawEntity).Archetype.ComponentTypes.HasAll(ClientComponents.Resolve(components));

    public bool IsAlive(RawEntity rawEntity)
        => !_scope.IsPending(rawEntity) && !_store.GetEntityByRawEntity(rawEntity).IsNull;

    public bool Delete(RawEntity rawEntity)
    {
        if (!IsAlive(rawEntity))
            return false;

        if (_scope.InQuery)
            return _scope.Mark(rawEntity);

        DeleteNow(rawEntity);
        return true;
    }

    public void EnterQuery() => _scope.Enter();

    public void LeaveQuery()
    {
        if (!_scope.Leave())
            return;

        foreach (var rawEntity in _scope.Pending)
            DeleteNow(rawEntity);

        _scope.Clear();
    }

    private void DeleteNow(RawEntity rawEntity)
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);

        if (!entity.IsNull)
            entity.DeleteEntity();
    }

    private Entity Resolve(RawEntity rawEntity)
    {
        if (_scope.IsPending(rawEntity))
            throw new InvalidEntityException();

        var entity = _store.GetEntityByRawEntity(rawEntity);

        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity;
    }
}
