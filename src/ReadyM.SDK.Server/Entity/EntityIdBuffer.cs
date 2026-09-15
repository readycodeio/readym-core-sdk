namespace ReadyM.SDK.Server.Entity;

/// The entity IDs a query matched when the loop started.
/// <remarks>
/// Nesting works because a query fills its buffer before the loop body runs, so an inner query rents a new one.
/// </remarks>
internal sealed class EntityIdBuffer
{
    [ThreadStatic]
    private static EntityIdBuffer? _free;

    private EntityIdBuffer? _next;
    private int[] _ids = new int[256];

    public int Count { get; private set; }

    public int this[int index] => _ids[index];

    public static EntityIdBuffer Rent()
    {
        var buffer = _free;

        if (buffer == null)
            return new EntityIdBuffer();

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

    public unsafe void Append(int* ids, int count)
    {
        var required = Count + count;

        if (required > _ids.Length)
        {
            var capacity = _ids.Length;
            while (capacity < required) capacity *= 2;
            Array.Resize(ref _ids, capacity);
        }

        fixed (int* destination = _ids)
        {
            Buffer.MemoryCopy(ids, destination + Count, (_ids.Length - Count) * sizeof(int), count * sizeof(int));
        }

        Count = required;
    }
}