using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Interop;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// CoreCLR-side owner of a typed component array. Exposes all mutations through delegates so
/// write barriers always fire correctly - even for non-blittable T. The AOT relay holds the
/// resulting AOTHeapPointers and dispatches through them; it never writes directly into the array.
/// </summary>
internal sealed class TypedComponentHeap<T> : IDisposable where T : struct
{
    private T[] _components;

    private GCHandle _selfHandle;

    // Delegate store keeps the GC from collecting delegates whose IntPtrs live in AOT memory.
    private readonly PinnedDelegateStore _delegateStore;

    // One field per delegate so the same instance is always registered in the store.
    private readonly HeapGetCountDelegate _dGetLength;
    private readonly HeapResizeDelegate _dResize;
    private readonly HeapMoveDelegate _dMove;
    private readonly HeapCopyToDelegate _dCopyTo;
    private readonly HeapSetDefaultDelegate _dSetDefault;
    private readonly HeapClearRangeDelegate _dSetRangeDefault;

    /// <summary>
    /// A tracked reference to an element, for walking the heap from inside this runtime. Unlike a
    /// raw address this survives a collection that relocates the array, because the GC rewrites the
    /// reference. Use it instead of a pointer whenever T may hold managed references.
    /// </summary>
    /// <remarks>
    /// It protects against relocation, not reallocation: a resize swaps in a new array, so a
    /// reference taken before one addresses the old array.
    /// </remarks>
    public ref T GetRef(int index) => ref _components[index];

    public TypedComponentHeap(int initialCapacity)
    {
        _components = new T[initialCapacity];
        _selfHandle = GCHandle.Alloc(this); // Normal - keeps instance rooted, not pinned

        _delegateStore = new PinnedDelegateStore();

        // Create delegates before pinning so each field captures exactly one closure instance.
        _dGetLength = GetLengthImpl;
        _dResize = ResizeImpl;
        _dMove = MoveImpl;
        _dCopyTo = CopyToImpl;
        _dSetDefault = SetDefaultImpl;
        _dSetRangeDefault = SetRangeDefaultImpl;

        _delegateStore.PinDelegate(_dGetLength);
        _delegateStore.PinDelegate(_dResize);
        _delegateStore.PinDelegate(_dMove);
        _delegateStore.PinDelegate(_dCopyTo);
        _delegateStore.PinDelegate(_dSetDefault);
        _delegateStore.PinDelegate(_dSetRangeDefault);
    }

    // -------------------------------------------------------------------------
    // Delegate implementations
    // -------------------------------------------------------------------------

    private int GetLengthImpl() => _components.Length;

    private void ResizeImpl(int newCapacity, int copyCount)
    {
        var newArray = new T[newCapacity];
        Array.Copy(_components, newArray, copyCount);
        _components = newArray;
    }

    private void MoveImpl(int from, int to)
    {
        _components[to] = _components[from]; // write barrier fires on each assignment
        _components[from] = default;
    }

    private void CopyToImpl(int srcPos, IntPtr targetSelf, int dstPos)
    {
        // targetSelf is the GCHandle IntPtr of the target TypedComponentHeap<T>.
        // The ECS guarantees source and target are the same component type, so the cast is safe.
        var target = (TypedComponentHeap<T>)GCHandle.FromIntPtr(targetSelf).Target!;
        target._components[dstPos] = _components[srcPos]; // write barrier fires
    }

    private void SetDefaultImpl(int index) => _components[index] = default;

    private void SetRangeDefaultImpl(int start, int count) => Array.Clear(_components, start, count);

    // -------------------------------------------------------------------------
    // Managed-side accessors (for use from other CoreCLR code, not AOT)
    // -------------------------------------------------------------------------

    public T GetComponent(int index) => _components[index];
    public void SetComponent(int index, T value) => _components[index] = value; // write barrier fires

    // -------------------------------------------------------------------------
    // Registration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the pointer bundle to hand to the AOT relay for this heap.
    /// The returned pointers are stable for the lifetime of this instance.
    /// </summary>
    public AOTHeapPointers GetPointers() => new()
    {
        Self = GCHandle.ToIntPtr(_selfHandle),
        Stride = Unsafe.SizeOf<T>(),
        GetLength = _delegateStore.PinDelegate(_dGetLength),
        Resize = _delegateStore.PinDelegate(_dResize),
        Move = _delegateStore.PinDelegate(_dMove),
        CopyTo = _delegateStore.PinDelegate(_dCopyTo),
        SetDefault = _delegateStore.PinDelegate(_dSetDefault),
        SetRangeDefault = _delegateStore.PinDelegate(_dSetRangeDefault),
    };

    // -------------------------------------------------------------------------
    // Lifetime
    // -------------------------------------------------------------------------

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _delegateStore.Dispose();
        if (_selfHandle.IsAllocated) _selfHandle.Free();
    }

    ~TypedComponentHeap() => Dispose();
}

// NOTE - per-heap disposal:
// If archetypes are destroyed at runtime and you want to reclaim memory promptly, add a
// DisposeHeap delegate to AOTHeapPointers (IntPtr to void, taking the Self GCHandle IntPtr),
// implement it as:
//
//   private void DisposeHeapImpl(IntPtr selfHandle)
//   {
//       var heap = (IDisposable)GCHandle.FromIntPtr(selfHandle).Target!;
//       heap.Dispose();
//       lock (_heapsLock) _allHeaps.Remove((IDisposable)heap); // optional bookkeeping
//   }
//
// Call it from ExternallyManagedHeap's finalizer on the AOT side, mirroring the original
// drop-delegate pattern. For now bulk disposal on world shutdown is sufficient.