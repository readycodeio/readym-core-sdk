using Friflo.Engine.ECS;

namespace ReadyM.SDK.Client;

public class EntityApi(EntityStore store) : IEntityApi
{
    public bool HasComponent<T>(RawEntity rawEntity) where T : struct, IComponent
    {
        var entity = store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity.HasComponent<T>();
    }

    public ref T GetComponent<T>(RawEntity rawEntity) where T : struct, IComponent
    {
        var entity = store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return ref entity.GetComponent<T>();
    }

    public bool TryGetComponent<T>(RawEntity rawEntity, out T component) where T : struct, IComponent
    {
        var entity = store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity.TryGetComponent(out component);
    }

    public void AddComponent<T>(RawEntity rawEntity) where T : struct, IComponent
    {
        var entity = store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        entity.AddComponent<T>();
    }

    public bool IsAlive(RawEntity rawEntity)
    {
        return !store.GetEntityByRawEntity(rawEntity).IsNull;
    }

    public bool HasTag<T>(RawEntity rawEntity) where T : struct, ITag
    {
        var entity = store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity.Tags.Has<T>();
    }

    public void SetTag<T>(RawEntity rawEntity, bool set) where T : struct, ITag
    {
        var entity = store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        if (set)
        {
            entity.AddTag<T>();
        }
        else
        {
            entity.RemoveTag<T>();
        }
    }
}