using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Entity;

public interface IEntities
{
    QueryBuilder<T> Query<T>() where T : struct, IArchetypeQueryable;

    /// <summary>Create a new entity of a given Archetype.</summary>
    T Create<T>() where T : struct, IArchetype;

    /// <summary>Delete an entity. False when it was already gone.</summary>
    bool Delete<T>(in T shape) where T : struct, IArchetype;
    QueryBuilder<T1, T2> Query<T1, T2>() where T1 : struct, IArchetypeMixin where T2 : struct, IArchetypeMixin;
    // TODO: Write or generate Query methods for up to 6 parameters
}