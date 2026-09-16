using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity.Chunks;

/// <summary>
/// A query that walks chunks rather than identities: one crossing for the whole query instead of one
/// per value a body reads.
/// </summary>
/// <remarks>
/// The trade against <see cref="QueryBuilder{T}"/> is that the view is bound to the world as it was
/// when the loop started, so nothing inside may change an entity's shape. Reading and writing
/// component values is free; anything structural goes through <c>Handle</c> and happens after.
/// </remarks>
public readonly ref struct ChunkQuery<TView>
    where TView : IArchetypeChunkView<TView>, allows ref struct
{
    private readonly ServerEntityApi _api;

    internal ChunkQuery(ServerEntityApi api) => _api = api;

    public Enumerator GetEnumerator() => new(_api);

    public ref struct Enumerator
    {
        private readonly ChunkBuffer _buffer;
        private readonly EntityHandle _prototype;

        private TView _chunk;
        private int _chunkIndex;
        private int _index;
        private int _count;

        internal Enumerator(ServerEntityApi api)
        {
            _buffer = api.CollectChunks(TView.Components);
            _prototype = new EntityHandle(default, api);
            _chunk = default!;
            _chunkIndex = -1;
            _index = 0;
            _count = 0;
        }

        public readonly TView Current => _chunk.At(_index);

        /// Walks entities within a chunk, and binds the next chunk when one runs out.
        public bool MoveNext()
        {
            while (++_index >= _count)
            {
                if (++_chunkIndex >= _buffer.ChunkCount)
                    return false;

                _count = _buffer.CountOf(_chunkIndex);
                _chunk = TView.Bind(_buffer.SlotsOf(_chunkIndex), _buffer.EntitiesOf(_chunkIndex), _prototype);
                _index = -1;
            }

            return true;
        }

        public readonly void Dispose() => _buffer.Return();
    }
}
