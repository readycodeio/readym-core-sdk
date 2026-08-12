using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Interop;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// CoreCLR-side owner of a typed component array. Exposes all mutations through delegates so
/// write barriers always fire correctly - even for non-blittable T. The AOT relay holds the
/// resulting AOTHeapPointers and dispatches through them; it never writes directly into the array.
/// </summary>
internal sealed class TypedComponentHeap<T> : IDisposable where T : struct
{
    private T[] _components;

    private readonly bool _isBlittable;
    private GCHandle _selfHandle;
    private GCHandle _arrayPinHandle;

    // Delegate store keeps the GC from collecting delegates whose IntPtrs live in AOT memory.
    private readonly PinnedDelegateStore _delegateStore;

    // One field per delegate so the same instance is always registered in the store.
    private readonly HeapGetPtrDelegate     _dGetPtrToFirst;
    private readonly HeapGetCountDelegate   _dGetLength;
    private readonly HeapResizeDelegate     _dResize;
    private readonly HeapMoveDelegate       _dMove;
    private readonly HeapCopyToDelegate     _dCopyTo;
    private readonly HeapSetDefaultDelegate _dSetDefault;
    private readonly HeapClearRangeDelegate _dSetRangeDefault;

    public int Stride => Unsafe.SizeOf<T>();
    public bool IsBlittable => _isBlittable;

    public TypedComponentHeap(int initialCapacity)
    {
        _isBlittable = CheckBlittable();
        _components = new T[initialCapacity];
        _selfHandle = GCHandle.Alloc(this); // Normal - keeps instance rooted, not pinned

        if (_isBlittable)
            _arrayPinHandle = GCHandle.Alloc(_components, GCHandleType.Pinned);

        _delegateStore = new PinnedDelegateStore();

        // Create delegates before pinning so each field captures exactly one closure instance.
        _dGetPtrToFirst = GetPtrToFirstImpl;
        _dGetLength = GetLengthImpl;
        _dResize = ResizeImpl;
        _dMove = MoveImpl;
        _dCopyTo = CopyToImpl;
        _dSetDefault = SetDefaultImpl;
        _dSetRangeDefault = SetRangeDefaultImpl;

        _delegateStore.PinDelegate(_dGetPtrToFirst);
        _delegateStore.PinDelegate(_dGetLength);
        _delegateStore.PinDelegate(_dResize);
        _delegateStore.PinDelegate(_dMove);
        _delegateStore.PinDelegate(_dCopyTo);
        _delegateStore.PinDelegate(_dSetDefault);
        _delegateStore.PinDelegate(_dSetRangeDefault);
    }

    private static bool CheckBlittable()
    {
        try
        {
            var h = GCHandle.Alloc(new T[1], GCHandleType.Pinned);
            h.Free();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Delegate implementations
    // -------------------------------------------------------------------------

    private unsafe IntPtr GetPtrToFirstImpl()
    {
        if (_isBlittable)
        {
            // Pinned - stable for the heap's lifetime, safe to cache on the AOT side.
            return _arrayPinHandle.IsAllocated
                ? _arrayPinHandle.AddrOfPinnedObject()
                : IntPtr.Zero;
        }

        // Non-blittable: live pointer, only valid during a no-GC region.
        // GetArrayDataReference gives a ref to element[0] without requiring a pin.
        // Unsafe.AsPointer loses GC tracking, but the caller (ScanArchetypes) holds
        // a no-GC region for the duration of the callback, so the array cannot move.
        return (IntPtr)Unsafe.AsPointer(
            ref MemoryMarshal.GetArrayDataReference(_components));
    }

    private int GetLengthImpl() => _components.Length;

    private void ResizeImpl(int newCapacity, int copyCount)
    {
        var newArray = new T[newCapacity];
        Array.Copy(_components, newArray, copyCount);

        if (_isBlittable)
        {
            // Pin new array before releasing old pin - no window where GetPtrToFirst is invalid.
            var oldPin = _arrayPinHandle;
            _arrayPinHandle = GCHandle.Alloc(newArray, GCHandleType.Pinned);
            _components = newArray;
            if (oldPin.IsAllocated) oldPin.Free();
        }
        else
        {
            _components = newArray;
        }
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
        Stride = Stride,
        IsBlittable = _isBlittable ? (byte)1 : (byte)0,
        GetPtrToFirst = _delegateStore.PinDelegate(_dGetPtrToFirst),
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
        if (_arrayPinHandle.IsAllocated) _arrayPinHandle.Free();
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