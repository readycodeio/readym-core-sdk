using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

public class Entities(EntityStore store, IEntityApi api) : IEntities
{
    public QueryBuilder<T> Query<T>()
        where T : struct, IArchetypeQueryable
        => new(store, api);

    public QueryBuilder<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable where T2 : struct, IArchetypeQueryable
        => new(store, api);
}