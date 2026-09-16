using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Chunks;

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

        internal Enumerator(IChunkSource source)
        {
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

        public readonly void Dispose() => _buffer.Return();
    }
}

/// <summary>
/// Whichever half is behind a query: it knows how to find the chunks a shape matches.
/// </summary>
/// <remarks>
/// The two halves find them very differently, one by asking the relay across the interop boundary
/// and one by walking the store it already has, but a loop over the result is the same either way,
/// which is what lets one generated view serve both.
/// </remarks>
public interface IChunkSource
{
    internal ChunkBuffer Collect(ComponentSet components);

    /// <summary>A handle carrying this half's api, which a view rebases onto each entity.</summary>
    internal EntityHandle Prototype { get; }
}
