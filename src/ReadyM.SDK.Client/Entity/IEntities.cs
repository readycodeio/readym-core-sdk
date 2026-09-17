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

    /// <summary>
    /// Removes the entity a shape names: an archetype, a mixin, or, where the runtime has them, the
    /// chunk view of either. False when it was already gone.
    /// </summary>
    /// <remarks>
    /// Inside a query the removal is held until the loop ends. The anti-constraint is the only part
    /// that differs by target, and a netstandard2.0 build has no chunk views to pass anyway.
    /// </remarks>
#if NET
    bool Delete<T>(in T shape) where T : IEntityShape, allows ref struct;
#else
    bool Delete<T>(in T shape) where T : IEntityShape;
#endif

    /// <summary>
    /// The same, by handle. What a loop walking chunks uses, since a view cannot be a type argument.
    /// </summary>
    bool Delete(EntityHandle handle);
    /// Every entity carrying all 2 shapes, walked the same way a single one is.
    EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin;

    /// Every entity carrying all 3 shapes, walked the same way a single one is.
    EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin;

    /// Every entity carrying all 4 shapes, walked the same way a single one is.
    EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin;

    /// Every entity carrying all 5 shapes, walked the same way a single one is.
    EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin;

    /// Every entity carrying all 6 shapes, walked the same way a single one is.
    EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin;
}