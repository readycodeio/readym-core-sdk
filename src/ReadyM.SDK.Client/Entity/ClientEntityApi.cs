using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

internal sealed class ClientEntityApi : IEntityApi
{
    private readonly EntityStore _store;

    public ClientEntityApi(EntityStore store) => _store = store;

    public bool HasComponent<T>(RawEntity rawEntity) where T : struct, IComponent
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity.HasComponent<T>();
    }

    public ref T GetComponent<T>(RawEntity rawEntity) where T : struct, IComponent
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return ref entity.GetComponent<T>();
    }

    public bool TryGetComponent<T>(RawEntity rawEntity, out T component) where T : struct, IComponent
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity.TryGetComponent(out component);
    }

    public void AddComponent<T>(RawEntity rawEntity) where T : struct, IComponent
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        entity.AddComponent<T>();
    }

    public bool HasComponents(RawEntity rawEntity, ComponentSet components)
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity.Archetype.ComponentTypes.HasAll(ClientComponents.Resolve(components));
    }

    public bool IsAlive(RawEntity rawEntity)
    {
        return !_store.GetEntityByRawEntity(rawEntity).IsNull;
    }

}