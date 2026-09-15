using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

internal class ClientEntityApi : IEntityApi
{
    private readonly CommandBuffer _buffer;
    private readonly EntityStore _store;

    public ClientEntityApi(EntityStore store)
    {
        _store = store;

        _buffer = store.GetCommandBuffer();
        _buffer.ReuseBuffer = true;
    }

    public void Playback() => _buffer.Playback();

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
        
        _buffer.AddComponent<T>(rawEntity.Id);
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

    public bool HasTag<T>(RawEntity rawEntity) where T : struct, ITag
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity.Tags.Has<T>();
    }

    public void SetTag<T>(RawEntity rawEntity, bool set) where T : struct, ITag
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);
        if (entity.IsNull)
            throw new InvalidEntityException();

        if (set)
        {
            _buffer.AddTag<T>(rawEntity.Id);
        }
        else
        {
            _buffer.RemoveTag<T>(rawEntity.Id);
        }
    }
}