using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Entity;

public interface IEntities
{
    QueryBuilder<T> Query<T>() where T : struct, IArchetypeQueryable;
    QueryBuilder<T1, T2> Query<T1, T2>() where T1 : struct, IArchetypeQueryable where T2 : struct, IArchetypeQueryable;
}