using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Entities;

internal class ServerEntities(ServerEntityApi api) : IEntities
{
    public EntityQuery<T> Query<T>()
        where T : struct, IArchetypeQueryable
        => new(api);

    public T Create<T>()
        where T : struct, IArchetype
        => new() { Handle = new EntityHandle(api.Create(ArchetypeRegistry.SetFor(typeof(T), default(T).Components)), api) };

    public bool Delete<T>(in T shape)
        where T : IEntityShape, allows ref struct
        => api.Delete(shape.Handle.RawEntity);

    public EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        => new(api);

    public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        => new(api);

    public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        => new(api);

    public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        => new(api);

    public EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin
        => new(api);

    public bool TryLookup<T, TKey>(TKey key, out T shape) where T : struct, IArchetypeQueryable, IIndexed<TKey>
    {
        if (IndexRegistry.TryFind<T, TKey>(api, key, out var entity))
        {
            shape = new T { Handle = new EntityHandle(entity, api) };
            return true;
        }

        shape = default;
        return false;
    }

    public T Create<T>(Scope scope) where T : struct, IArchetype
        => new() { Handle = new EntityHandle(api.Create(ArchetypeRegistry.SetFor(typeof(T), default(T).Components), scope.Handle.RawEntity), api) };
}
