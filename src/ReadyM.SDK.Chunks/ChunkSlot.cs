using System.ComponentModel;

namespace ReadyM.SDK.Chunks;

/// <summary>
/// Where one component of one chunk lives, in whichever form the owning side can offer.
/// </summary>
/// <remarks>
/// Deliberately not a ref struct: a query collects one of these per component per chunk, and that
/// needs an array. Turning it into a reference is <see cref="ComponentChunk"/>'s job, and only
/// happens once a chunk is actually being walked.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ChunkSlot
{
    internal readonly Array? Heap;
    internal readonly IntPtr Data;
    internal readonly int Stride;

    /// An array the managed side owns. Its elements may hold references, so it is never pinned.
    internal ChunkSlot(Array heap, int stride)
    {
        Heap = heap;
        Data = IntPtr.Zero;
        Stride = stride;
    }

    /// Memory that does not move, so an address is enough.
    internal ChunkSlot(IntPtr data, int stride)
    {
        Heap = null;
        Data = data;
        Stride = stride;
    }

    internal bool IsEmpty => Heap is null && Data == IntPtr.Zero;
}
