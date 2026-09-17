using System.ComponentModel;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
#if NET
using ReadyM.SDK.Chunks;
#endif

namespace ReadyM.SDK.Client.Entity;

/// <summary>
/// Every entity of a shape. How it is walked is decided for you: a shape whose components are all
/// known at compile time is walked by chunk, and anything else by identity.
/// </summary>
/// <remarks>
/// The choice is made by overload resolution, not at run time. This type deliberately has no
/// GetEnumerator of its own, so a foreach binds one of the extensions: the generated one when the
/// shape has a chunk view, and the general one otherwise.
///
/// A netstandard2.0 client has no chunk path at all, since it has neither ref fields nor
/// `allows ref struct`, so every query there walks identities and the generator emits no view.
/// </remarks>
public readonly struct EntityQuery<T> where T : struct, IArchetypeQueryable
{
    private readonly ClientEntityContext _context;

    internal EntityQuery(ClientEntityContext context) => _context = context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public Query<T>.Enumerator Identities() => new Query<T>(_context.Store, _context.Api).GetEnumerator();

    /// <summary>Runs a body over every entity, walking identities so it may change the world.</summary>
    public void ForEach(Action<T> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
            body(entities.Current);

        entities.Dispose();
    }

#if NET
    /// <summary>What a generated overload calls, so the source itself stays off the public surface.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_context.ChunkSource).GetEnumerator();
#endif
}

/// <summary>Two shapes at once, without an archetype that names both.</summary>
public readonly struct EntityQuery<T1, T2>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeQueryable
{
    private readonly ClientEntityContext _context;

    internal EntityQuery(ClientEntityContext context) => _context = context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public Query<T1, T2>.Enumerator Identities()
        => new Query<T1, T2>(_context.Store, _context.Api).GetEnumerator();

    /// <summary>Runs a body over every entity, walking identities so it may change the world.</summary>
    public void ForEach(Action<T1, T2> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
        {
            var (first, second) = entities.Current;
            body(first, second);
        }

        entities.Dispose();
    }

#if NET
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_context.ChunkSource).GetEnumerator();
#endif
}

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
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeQueryable
        => query.Identities();
}
