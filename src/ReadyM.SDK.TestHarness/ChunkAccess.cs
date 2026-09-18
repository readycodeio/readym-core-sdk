using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs;

namespace ReadyM.SDK.TestHarness;

/// <summary>
/// Turns a chunk slot into a reference, whichever side owns the array.
/// </summary>
/// <remarks>
/// The same two cases <c>EcsApi.ChunkBase</c> already handles: a mod-owned slot carries its heap's
/// handle rather than an address, because the component may hold managed references and so can never
/// be pinned, and resolving it here yields a reference the GC tracks. An AOT-owned slot is already
/// pinned, so its address is used as-is.
/// </remarks>
internal static class ChunkAccess
{
    internal static unsafe ref T Base<T>(in ChunkComponent comp) where T : struct
    {
        if (comp.HeapSelf != IntPtr.Zero)
            return ref ((TypedComponentHeap<T>)GCHandle.FromIntPtr(comp.HeapSelf).Target!).GetRef(0);

        return ref Unsafe.AsRef<T>((void*)comp.Data);
    }

    /// Advances by the stride the owning side reported, not by this side's view of the struct size.
    internal static ref T At<T>(ref T first, int index, int stride) where T : struct
        => ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Unsafe.As<T, byte>(ref first), (nint)index * stride));
}
