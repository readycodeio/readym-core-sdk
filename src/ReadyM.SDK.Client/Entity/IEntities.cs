using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Entity;

public interface IEntities
{
    QueryBuilder<T> Query<T>() where T : struct, IArchetype;
}