using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Server.Entity;

public interface IEntities
{
    /// <summary>
    /// Every entity of a shape. Walked by chunk when the shape allows it, by identity otherwise,
    /// which is decided when the loop is compiled rather than when it runs.
    /// </summary>
    EntityQuery<T> Query<T>() where T : struct, IArchetypeQueryable;

    /// <summary>Every entity carrying all 2 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable;
    /// <summary>Every entity carrying all 3 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        where T3 : struct, IArchetypeQueryable;
    /// <summary>Every entity carrying all 4 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        where T3 : struct, IArchetypeQueryable
        where T4 : struct, IArchetypeQueryable;
    /// <summary>Every entity carrying all 5 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        where T3 : struct, IArchetypeQueryable
        where T4 : struct, IArchetypeQueryable
        where T5 : struct, IArchetypeQueryable;
    /// <summary>Every entity carrying all 6 shapes, walked the same way a single one is.</summary>
    EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        where T3 : struct, IArchetypeQueryable
        where T4 : struct, IArchetypeQueryable
        where T5 : struct, IArchetypeQueryable
        where T6 : struct, IArchetypeQueryable;
}
