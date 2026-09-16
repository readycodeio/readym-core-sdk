using System.ComponentModel;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Chunks;

namespace ReadyM.SDK.Server.Entity;

/// <summary>
/// Every entity of a shape. How it is walked is decided for you: a shape whose components are all
/// known at compile time is walked by chunk, and anything else by identity.
/// </summary>
/// <remarks>
/// The choice is made by overload resolution, not at run time. This type deliberately has no
/// GetEnumerator of its own, so a foreach binds one of the extensions: the generated one when the
/// shape has a chunk view, and the general one otherwise. That is what lets the element type differ
/// between the two without the loop body ever having to.
/// </remarks>
public readonly ref struct EntityQuery<T> where T : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;

    internal EntityQuery(ServerEntityApi api) => _api = api;

    /// <summary>What a generated overload calls, so the api itself stays off the public surface.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public QueryBuilder<T>.Enumerator Identities() => new QueryBuilder<T>(_api).GetEnumerator();


}
