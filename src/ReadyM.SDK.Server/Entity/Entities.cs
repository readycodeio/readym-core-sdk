using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Server.Entity;

internal class Entities(ServerEntityApi api) : IEntities
{
    public QueryBuilder<T> Query<T>()
        where T : struct, IArchetypeQueryable
        => new(api);
}
