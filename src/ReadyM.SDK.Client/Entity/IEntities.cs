using ReadyM.SDK.Entity;
using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Entity;

public interface IEntities
{
    /// <summary>
    /// Every entity of a shape. Walked by chunk when the shape and the runtime allow it, by identity
    /// otherwise, which is decided when the loop is compiled rather than when it runs.
    /// </summary>
    EntityQuery<T> Query<T>() where T : struct, IArchetypeQueryable;

    /// <summary>Create a new entity of a given Archetype.</summary>
    T Create<T>() where T : struct, IArchetype;

    /// <summary>Delete an entity. False when it was already gone.</summary>
    bool Delete<T>(in T shape) where T : struct, IArchetype;

    /// <summary>
    /// The same, by handle. What a loop walking chunks uses, since a view cannot be a type argument.
    /// </summary>
    bool Delete(EntityHandle handle);
    EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable;
    // TODO: Write or generate Query methods for up to 6 parameters
}