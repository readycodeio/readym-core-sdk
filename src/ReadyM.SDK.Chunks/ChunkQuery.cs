using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Chunks;

/// A query that walks chunks rather than entity IDs.
public readonly ref struct ChunkQuery<TView>
    where TView : IArchetypeChunkView<TView>, allows ref struct
{
    private readonly IChunkSource _source;

    internal ChunkQuery(IChunkSource source) => _source = source;

    public Enumerator GetEnumerator() => new(_source);

    public ref struct Enumerator
    {
        private readonly ChunkBuffer _buffer;
        private readonly EntityHandle _prototype;

        private TView _chunk;
        private int _chunkIndex;
        private int _index;
        private int _count;

        private readonly IChunkSource _source;

        internal Enumerator(IChunkSource source)
        {
            source.EnterQuery();

            _source = source;
            _buffer = source.Collect(TView.Components);
            _prototype = source.Prototype;
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

        public readonly void Dispose()
        {
            _buffer.Return();
            _source.LeaveQuery();
        }
    }
}