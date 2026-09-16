using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Server.Entity;

internal class Entities(ServerEntityApi api) : IEntities
{
    public EntityQuery<T> Query<T>()
        where T : struct, IArchetypeQueryable
        => new(api);

    public EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        => new(api);
    public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        where T3 : struct, IArchetypeQueryable
        => new(api);
    public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        where T3 : struct, IArchetypeQueryable
        where T4 : struct, IArchetypeQueryable
        => new(api);
    public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        where T3 : struct, IArchetypeQueryable
        where T4 : struct, IArchetypeQueryable
        where T5 : struct, IArchetypeQueryable
        => new(api);
    public EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        where T3 : struct, IArchetypeQueryable
        where T4 : struct, IArchetypeQueryable
        where T5 : struct, IArchetypeQueryable
        where T6 : struct, IArchetypeQueryable
        => new(api);
}
