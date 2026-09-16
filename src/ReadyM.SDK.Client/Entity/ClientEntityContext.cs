using Friflo.Engine.ECS;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

/// <summary>
/// What a query needs from the client half: the store, the api handles resolve through, and, where
/// the runtime allows one, the source that finds chunks.
/// </summary>
/// <remarks>
/// One object rather than three fields on every query, because a query is a struct handed around by
/// value and the chunk source only exists on some targets.
/// </remarks>
internal sealed class ClientEntityContext(EntityStore store, IEntityApi api)
{
    internal EntityStore Store { get; } = store;

    internal IEntityApi Api { get; } = api;

#if NET
    internal Chunks.IChunkSource ChunkSource { get; } = new ClientChunkSource(store, api);
#endif
}
