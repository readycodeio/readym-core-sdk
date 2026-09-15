using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Entity;

public interface IEntities
{
    QueryBuilder<T> Query<T>() where T : struct, IArchetypeQueryable;
    QueryBuilder<T1, T2> Query<T1, T2>() where T1 : struct, IArchetypeMixin where T2 : struct, IArchetypeMixin;
    // TODO: Write or generate Query methods for up to 6 parameters
}