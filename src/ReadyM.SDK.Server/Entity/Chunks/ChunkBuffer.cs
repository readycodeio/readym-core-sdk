using Friflo.Engine.ECS;

namespace ReadyM.SDK.Server.Entity.Chunks;

/// <summary>
/// The chunks a query matched, captured while the owning side was handing them out.
/// </summary>
/// <remarks>
/// The owning side pushes chunks at a callback, and a foreach has to pull, so the descriptors are
/// collected first and walked after. There are only as many as there are matching archetypes, so
/// this stays small however many entities the query matched. Nesting works the same way it does for
/// the identity buffer: a query fills its own before any loop body runs.
/// </remarks>
internal sealed class ChunkBuffer
{
    [ThreadStatic]
    private static ChunkBuffer? _free;

    private ChunkBuffer? _next;
    private ChunkSlot[] _slots = new ChunkSlot[64];
    private IntPtr[] _entities = new IntPtr[8];
    private int[] _counts = new int[8];

    /// How many components each chunk carries, which is how many the query asked for.
    internal int Width { get; private set; }

    internal int ChunkCount { get; private set; }

    internal static ChunkBuffer Rent(int width)
    {
        var buffer = _free ?? new ChunkBuffer();

        _free = buffer._next;
        buffer._next = null;
        buffer.ChunkCount = 0;
        buffer.Width = width;

        return buffer;
    }

    internal void Return()
    {
        // Dropping the array references keeps a returned buffer from rooting a heap it is done with.
        Array.Clear(_slots, 0, ChunkCount * Width);

        ChunkCount = 0;
        _next = _free;
        _free = this;
    }

    internal Span<ChunkSlot> Append(IntPtr entities, int count)
    {
        if (ChunkCount == _counts.Length)
        {
            Array.Resize(ref _counts, _counts.Length * 2);
            Array.Resize(ref _entities, _entities.Length * 2);
        }

        var start = ChunkCount * Width;

        if (start + Width > _slots.Length)
            Array.Resize(ref _slots, Math.Max(_slots.Length * 2, start + Width));

        _entities[ChunkCount] = entities;
        _counts[ChunkCount] = count;
        ChunkCount++;

        return new Span<ChunkSlot>(_slots, start, Width);
    }

    internal int CountOf(int chunk) => _counts[chunk];

    internal ReadOnlySpan<ChunkSlot> SlotsOf(int chunk) => new(_slots, chunk * Width, Width);

    internal unsafe ReadOnlySpan<RawEntity> EntitiesOf(int chunk)
        => new((void*)_entities[chunk], _counts[chunk]);
}
