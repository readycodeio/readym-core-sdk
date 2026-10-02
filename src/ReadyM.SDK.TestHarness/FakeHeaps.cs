using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs;

namespace ReadyM.SDK.TestHarness;

/// <summary>
/// One component's storage inside the fake relay, in whichever of the two kinds the ABI allows.
/// </summary>
internal abstract class FakeHeap
{
    internal int Stride { get; init; }

    internal abstract int Add();

    internal abstract ComponentSlot SlotAt(int row);

    internal abstract ChunkComponent Chunk();

    internal abstract void SetValue(int row, object value);

    internal abstract object GetValue(int row);

    /// Deleting swaps the last row into the hole, which is what keeps a chunk contiguous.
    internal void Move(int from, int to) => SetValue(to, GetValue(from));
}

/// <summary>
/// A heap the AOT side owns: blittable, pinned, addressed by pointer. <c>HeapSelf</c> stays zero.
/// </summary>
internal sealed class PinnedHeap<T> : FakeHeap where T : struct
{
    private T[] _items = GC.AllocateArray<T>(8, pinned: true);
    private int _count;

    internal PinnedHeap() => Stride = Unsafe.SizeOf<T>();

    internal override int Add()
    {
        if (_count == _items.Length)
        {
            var bigger = GC.AllocateArray<T>(_items.Length * 2, pinned: true);
            _items.AsSpan(0, _count).CopyTo(bigger);
            _items = bigger;
        }

        return _count++;
    }

    internal override unsafe ComponentSlot SlotAt(int row) => new()
    {
        Data = (IntPtr)Unsafe.AsPointer(ref _items[row]),
        HeapSelf = IntPtr.Zero,
        Index = row
    };

    internal override unsafe ChunkComponent Chunk() => new()
    {
        Data = (IntPtr)Unsafe.AsPointer(ref _items[0]),
        HeapSelf = IntPtr.Zero,
        Stride = Stride
    };

    internal override void SetValue(int row, object value) => _items[row] = (T)value;

    internal override object GetValue(int row) => _items[row];
}

/// <summary>
/// A heap a mod owns: may hold managed references, so it is addressed by heap handle and index and
/// never by address. This is the branch <c>ServerEntityApi.SlotRef</c> takes when HeapSelf is set.
/// </summary>
internal sealed class ManagedHeap<T> : FakeHeap where T : struct
{
    private const int InitialCapacity = 64;

    private readonly TypedComponentHeap<T> _heap = new(InitialCapacity);
    private readonly HeapResizeDelegate _resize;
    private readonly IntPtr _self;
    private int _capacity = InitialCapacity;
    private int _count;

    internal ManagedHeap()
    {
        var pointers = _heap.GetPointers();

        _self = pointers.Self;
        _resize = Marshal.GetDelegateForFunctionPointer<HeapResizeDelegate>(pointers.Resize);
        Stride = Unsafe.SizeOf<T>();
    }

    /// Grows through the heap's own resize delegate, which is how the relay grows a mod-owned heap.
    internal override int Add()
    {
        if (_count == _capacity)
        {
            _resize(_capacity * 2, _count);
            _capacity *= 2;
        }

        return _count++;
    }

    internal override ComponentSlot SlotAt(int row) => new()
    {
        Data = IntPtr.Zero,
        HeapSelf = _self,
        Index = row
    };

    internal override ChunkComponent Chunk() => new()
    {
        Data = IntPtr.Zero,
        HeapSelf = _self,
        Stride = Stride
    };

    internal override void SetValue(int row, object value) => _heap.SetComponent(row, (T)value);

    internal override object GetValue(int row) => _heap.GetComponent(row);
}
