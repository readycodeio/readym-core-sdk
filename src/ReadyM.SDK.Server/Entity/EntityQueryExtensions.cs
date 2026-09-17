using System.ComponentModel;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Server.Entity;

/// <summary>
/// The general way to walk a query, which every shape has. A generated overload takes precedence for
/// a shape that can be walked by chunk, because a non-generic candidate beats a generic one.
/// </summary>
public static class EntityQueryExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public static Query<T>.Enumerator GetEnumerator<T>(this EntityQuery<T> query)
        where T : struct, IArchetypeQueryable
        => query.Identities();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public static Query<T1, T2>.Enumerator GetEnumerator<T1, T2>(this EntityQuery<T1, T2> query)
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        => query.Identities();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public static Query<T1, T2, T3>.Enumerator GetEnumerator<T1, T2, T3>(this EntityQuery<T1, T2, T3> query)
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        => query.Identities();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public static Query<T1, T2, T3, T4>.Enumerator GetEnumerator<T1, T2, T3, T4>(this EntityQuery<T1, T2, T3, T4> query)
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        => query.Identities();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public static Query<T1, T2, T3, T4, T5>.Enumerator GetEnumerator<T1, T2, T3, T4, T5>(this EntityQuery<T1, T2, T3, T4, T5> query)
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        => query.Identities();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public static Query<T1, T2, T3, T4, T5, T6>.Enumerator GetEnumerator<T1, T2, T3, T4, T5, T6>(this EntityQuery<T1, T2, T3, T4, T5, T6> query)
        where T1 : struct, IArchetypeMixin
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin
        => query.Identities();
}