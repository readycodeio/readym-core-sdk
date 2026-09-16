using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

internal class Entities(EntityStore store, IEntityApi api) : IEntities
{
    private readonly ClientEntityContext _context = new(store, api);

    public EntityQuery<T> Query<T>()
        where T : struct, IArchetypeQueryable
        => new(_context);

    public T Create<T>() where T : struct, IArchetype
    {
        var archetype = store.GetArchetype(ClientComponents.Resolve(default(T).Components));
        return new T { Handle = new EntityHandle(archetype.CreateEntity().RawEntity, api) };
    }

    public bool Delete<T>(in T shape) where T : struct, IArchetype => Delete(EntityHandle.Of(shape));

    public bool Delete(EntityHandle handle)
    {
        var entity = store.GetEntityByRawEntity(handle.RawEntity);

        if (entity.IsNull)
            return false;

        entity.DeleteEntity();
        return true;
    }

    public EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        => new(_context);
}