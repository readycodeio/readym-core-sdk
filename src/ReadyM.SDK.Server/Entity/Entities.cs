using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity;

internal class Entities(ServerEntityApi api) : IEntities
{
    public EntityQuery<T> Query<T>()
        where T : struct, IArchetypeQueryable
        => new(api);

    public T Create<T>() where T : struct, IArchetype
        => new() { Handle = new EntityHandle(api.Create(default(T).Components), api) };

    public bool Delete<T>(in T shape) where T : struct, IArchetype
        => api.Delete(EntityHandle.Of(shape).RawEntity);

    public EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        => new(api);
    public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        => new(api);
    public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        => new(api);
    public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        => new(api);
    public EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin
        => new(api);
}
