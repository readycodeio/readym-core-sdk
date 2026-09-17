using System.ComponentModel;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Chunks;

namespace ReadyM.SDK.Server.Entity;

public readonly ref struct EntityQuery<T> where T : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public Query<T>.Enumerator Identities() => new Query<T>(_api).GetEnumerator();
}