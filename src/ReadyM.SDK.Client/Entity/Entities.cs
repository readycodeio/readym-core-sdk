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
        => new() { Handle = new EntityHandle(api.Create(default(T).Components), api) };

#if NET
    public bool Delete<T>(in T shape) where T : IEntityShape, allows ref struct => Delete(shape.Handle);
#else
    public bool Delete<T>(in T shape) where T : IEntityShape => Delete(shape.Handle);
#endif

    public bool Delete(EntityHandle handle) => api.Delete(handle.RawEntity);

    public EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        => new(_context);

    public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        => new(_context);

    public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        => new(_context);

    public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        => new(_context);

    public EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin
        => new(_context);
}