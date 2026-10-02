using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ReadyM.SDK.Chunks;

/// <summary>
/// One component's run of a chunk, as a reference the loop walks by index.
/// </summary>
/// <remarks>
/// A ref struct because the reference into a managed array has to be one the GC tracks: the array is
/// free to move while the loop runs, and only a tracked reference survives that. Which is also why a
/// view built on these can never be stored anywhere, which is the property we want.
///
/// Deliberately just the reference, with no stride alongside it. A view carries one of these per
/// component and is copied per entity, so every field here is paid for on every entity of every
/// query. The stride is recoverable anyway: only the assembly that declared a component can reach it
/// through <see cref="As{T}"/>, and it knows the size statically. The relay reports the same number,
/// since a mod-owned heap reports <c>Unsafe.SizeOf&lt;T&gt;()</c> and an AOT-owned one has to match
/// the mod's layout or nothing read through it would mean anything.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct ComponentChunk
{
    private readonly ref byte _origin;

    public unsafe ComponentChunk(in ChunkSlot slot)
        => _origin = ref slot.Heap is null
            ? ref Unsafe.AsRef<byte>((void*)slot.Data)
            : ref MemoryMarshal.GetArrayDataReference(slot.Heap);

    /// <summary>Reads the element as the component it is, without copying it.</summary>
    /// <remarks>
    /// Only the assembly that declared the component may call this, because only it can name the
    /// type. That is what keeps a component private while its accessors stay public, and what makes
    /// the element size known here without the chunk carrying it.
    /// </remarks>
    public ref T As<T>(int index) where T : struct
        => ref Unsafe.Add(ref Unsafe.As<byte, T>(ref _origin), index);
}
