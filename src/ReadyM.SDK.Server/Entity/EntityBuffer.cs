using Friflo.Engine.ECS;

namespace ReadyM.SDK.Server.Entity;

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

    public unsafe void Append(RawEntity* entities, int count)
    {
        var required = Count + count;

        if (required > _entities.Length)
        {
            var capacity = _entities.Length;
            while (capacity < required) capacity *= 2;
            Array.Resize(ref _entities, capacity);
        }

        fixed (RawEntity* destination = _entities)
        {
            var size = sizeof(RawEntity);
            Buffer.MemoryCopy(entities, destination + Count, (_entities.Length - Count) * size, count * size);
        }

        Count = required;
    }
}
