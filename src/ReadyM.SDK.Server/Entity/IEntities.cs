using ReadyM.SDK.Entity;
using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Server.Entity;

public interface IEntities
{
    /// <summary>
    /// Every entity of a shape. Walked by chunk when the shape allows it, by identity otherwise,
    /// which is decided when the loop is compiled rather than when it runs.
    /// </summary>
    EntityQuery<T> Query<T>() where T : struct, IArchetypeQueryable;

    /// <summary>
    /// A new entity carrying exactly what the shape needs.
    /// </summary>
    /// <remarks>
    /// Server-only for now: never replicated. The networked variants take a scope and an owner, and
    /// that shape is still open in the spec, so they are not here yet.
    /// </remarks>
    T Create<T>() where T : struct, IArchetype;

    /// <summary>Removes the entity behind a shape. False when it was already gone.</summary>
    bool Delete<T>(in T shape) where T : struct, IArchetype;

    /// <summary>Every entity carrying all 2 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin;

    /// <summary>Every entity carrying all 3 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin;

    /// <summary>Every entity carrying all 4 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin;

    /// <summary>Every entity carrying all 5 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin;

    /// <summary>Every entity carrying all 6 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin;
}