using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

public class Entities(EntityStore store, IEntityApi api) : IEntities
{
    public QueryBuilder<T> Query<T>() where T : struct, IArchetype => new(store, api);
}