using Friflo.Engine.ECS;

namespace ReadyM.SDK.Chunks;

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
    private int[]?[] _ids = new int[8][];
    private int[] _counts = new int[8];
    private int[] _starts = new int[8];
    private RawEntity[] _resolved = new RawEntity[256];
    private int _resolvedCount;

    // Set when the entities came as ids rather than as identities, which is how the client's store
    // hands them over. Resolving the revision is then one lookup, and only for entities a loop
    // actually asks about.
    private EntityStore? _store;

    /// How many components each chunk carries, which is how many the query asked for.
    internal int Width { get; private set; }

    internal int ChunkCount { get; private set; }

    internal void ResolveIdsThrough(EntityStore store) => _store = store;

    internal static ChunkBuffer Rent(int width)
    {
        var buffer = _free ?? new ChunkBuffer();

        _free = buffer._next;
        buffer._next = null;
        buffer.ChunkCount = 0;
        buffer.Width = width;
        buffer._store = null;
        buffer._resolvedCount = 0;

        return buffer;
    }

    internal void Return()
    {
        // Dropping the array references keeps a returned buffer from rooting a heap it is done with.
        Array.Clear(_slots, 0, ChunkCount * Width);
        Array.Clear(_ids, 0, ChunkCount);

        ChunkCount = 0;
        _resolvedCount = 0;
        _store = null;
        _next = _free;
        _free = this;
    }

    internal Span<ChunkSlot> Append(IntPtr entities, int count) => Append(entities, null, count);

    /// <summary>Entities as ids, which is what a store hands over.</summary>
    internal Span<ChunkSlot> Append(int[] ids, int count) => Append(IntPtr.Zero, ids, count);

    private Span<ChunkSlot> Append(IntPtr entities, int[]? ids, int count)
    {
        if (ChunkCount == _counts.Length)
        {
            Array.Resize(ref _counts, _counts.Length * 2);
            Array.Resize(ref _starts, _starts.Length * 2);
            Array.Resize(ref _entities, _entities.Length * 2);
            Array.Resize(ref _ids, _ids.Length * 2);
        }

        var start = ChunkCount * Width;

        if (start + Width > _slots.Length)
            Array.Resize(ref _slots, Math.Max(_slots.Length * 2, start + Width));

        _entities[ChunkCount] = entities;
        _ids[ChunkCount] = ids;
        _counts[ChunkCount] = count;
        _starts[ChunkCount] = _resolvedCount;

        if (ids is not null)
        {
            var required = _resolvedCount + count;

            if (required > _resolved.Length)
            {
                var capacity = _resolved.Length;
                while (capacity < required) capacity *= 2;
                Array.Resize(ref _resolved, capacity);
            }

            _resolvedCount = required;
        }

        ChunkCount++;

        return new Span<ChunkSlot>(_slots, start, Width);
    }

    internal int CountOf(int chunk) => _counts[chunk];

    internal ReadOnlySpan<ChunkSlot> SlotsOf(int chunk) => new(_slots, chunk * Width, Width);

    /// <summary>
    /// The entity at a position, however this half holds them.
    /// </summary>
    /// <remarks>
    /// The relay hands over identities that already carry their revision, so that side costs
    /// nothing. A store hands over ids, and the revision is looked up here: ids cannot go stale
    /// within a loop, because nothing structural may happen while one runs, but a handle taken out
    /// of the loop has to carry a revision or it would address whatever holds the id later.
    /// </remarks>
    internal unsafe ReadOnlySpan<RawEntity> EntitiesOf(int chunk)
    {
        var ids = _ids[chunk];

        if (ids is null)
            return new ReadOnlySpan<RawEntity>((void*)_entities[chunk], _counts[chunk]);

        var count = _counts[chunk];
        var start = _starts[chunk];

        for (var i = 0; i < count; i++)
            _resolved[start + i] = _store!.GetEntityById(ids[i]).RawEntity;

        return new ReadOnlySpan<RawEntity>(_resolved, start, count);
    }
}
