using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ReadyM.SDK.Server.Entity.Chunks;

/// <summary>
/// One component's run of a chunk, as a reference the loop walks by index.
/// </summary>
/// <remarks>
/// A ref struct because the reference into a managed array has to be one the GC tracks: the array is
/// free to move while the loop runs, and only a tracked reference survives that. Which is also why a
/// view built on these can never be stored anywhere, which is the property we want.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct ComponentChunk
{
    private readonly ref byte _origin;
    private readonly int _stride;

    public unsafe ComponentChunk(in ChunkSlot slot)
    {
        _stride = slot.Stride;
        _origin = ref slot.Heap is null
            ? ref Unsafe.AsRef<byte>((void*)slot.Data)
            : ref MemoryMarshal.GetArrayDataReference(slot.Heap);
    }

    /// The element at <paramref name="index"/>, by the stride the owning side reported.
    public ref byte At(int index) => ref Unsafe.Add(ref _origin, (nint)index * _stride);

    /// <summary>Reads the element as the component it is, without copying it.</summary>
    /// <remarks>
    /// Only the assembly that declared the component may call this, because only it can name the
    /// type. That is what keeps a component private while its accessors stay public.
    /// </remarks>
    public ref T As<T>(int index) where T : struct => ref Unsafe.As<byte, T>(ref At(index));
}
