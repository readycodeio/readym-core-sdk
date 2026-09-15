using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Server.Entity;

public interface IEntities
{
    QueryBuilder<T> Query<T>() where T : struct, IArchetypeQueryable;
    // QueryBuilder<T1, T2> Query<T1, T2>() where T1 : struct, IArchetypeQueryable where T2 : struct, IArchetypeQueryable;
    // TODO: Write or generate Query methods for up to 6 parameters
}