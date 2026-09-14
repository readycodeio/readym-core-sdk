using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Entity;

public interface IEntities
{
    QueryBuilder<T> Query<T>() where T : struct, IArchetype;
}