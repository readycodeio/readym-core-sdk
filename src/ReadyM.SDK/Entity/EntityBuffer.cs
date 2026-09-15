using Friflo.Engine.ECS;

namespace ReadyM.SDK.Entity;

/// The entities a query matched when the loop started.
/// <remarks>
/// Nesting works because a query fills its buffer before the loop body runs, so an inner query rents a different one.
/// </remarks>
internal sealed class EntityBuffer
{
    [ThreadStatic]
    private static EntityBuffer? _free;

    private EntityBuffer? _next;
    private RawEntity[] _entities = new RawEntity[256];

    public int Count { get; private set; }

    public RawEntity this[int index] => _entities[index];

    /// Snapshots what a query matched, so the loop body is free to change the world.
    public static EntityBuffer Fill(ArchetypeQuery query)
    {
        var buffer = Rent();

        foreach (var entity in query.Entities)
            buffer.Append(entity.RawEntity);

        return buffer;
    }

    public static EntityBuffer Rent()
    {
        var buffer = _free;

        if (buffer == null)
            return new EntityBuffer();

        _free = buffer._next;
        buffer._next = null;
        buffer.Count = 0;
        return buffer;
    }

    public void Return()
    {
        Count = 0;
        _next = _free;
        _free = this;
    }

    public void Append(RawEntity entity)
    {
        if (Count == _entities.Length)
            Array.Resize(ref _entities, _entities.Length * 2);

        _entities[Count++] = entity;
    }

    /// Reserves room for count more entities and returns the window to write them into.
    public Span<RawEntity> Reserve(int count)
    {
        var required = Count + count;

        if (required > _entities.Length)
        {
            var capacity = _entities.Length;
            while (capacity < required) capacity *= 2;
            Array.Resize(ref _entities, capacity);
        }

        var window = new Span<RawEntity>(_entities, Count, count);
        Count = required;
        return window;
    }
}
