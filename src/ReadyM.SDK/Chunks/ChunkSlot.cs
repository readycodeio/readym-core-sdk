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

    /// <summary>The chunk's first element in <see cref="Heap"/>; an address in <see cref="Data"/> points at it already.</summary>
    internal readonly int Start;

    /// <summary>An array the managed side owns. Its elements may hold references, so it is never pinned.</summary>
    /// <param name="start">Where the chunk starts in the array, when a dimension filter cut it into runs.</param>
    internal ChunkSlot(Array heap, int stride, int start = 0)
    {
        Heap = heap;
        Data = IntPtr.Zero;
        Stride = stride;
        Start = start;
    }

    /// <summary>Memory that does not move, so an address is enough.</summary>
    internal ChunkSlot(IntPtr data, int stride)
    {
        Heap = null;
        Data = data;
        Stride = stride;
        Start = 0;
    }

    internal bool IsEmpty => Heap is null && Data == IntPtr.Zero;
}
