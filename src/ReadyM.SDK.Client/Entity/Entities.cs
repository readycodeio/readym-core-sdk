using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

internal class Entities(EntityStore store, IEntityApi api) : IEntities
{
    public QueryBuilder<T> Query<T>()
        where T : struct, IArchetypeQueryable
        => new(store, api);

    public T Create<T>() where T : struct, IArchetype
    {
        var archetype = store.GetArchetype(ClientComponents.Resolve(default(T).Components));
        return new T { Handle = new EntityHandle(archetype.CreateEntity().RawEntity, api) };
    }

    public bool Delete<T>(in T shape) where T : struct, IArchetype
    {
        var entity = store.GetEntityByRawEntity(EntityHandle.Of(shape).RawEntity);

        if (entity.IsNull)
            return false;

        entity.DeleteEntity();
        return true;
    }

    public QueryBuilder<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeMixin where T2 : struct, IArchetypeMixin
        => new(store, api);
}