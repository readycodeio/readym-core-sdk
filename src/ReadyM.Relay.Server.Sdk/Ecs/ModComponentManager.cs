using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.Interop;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// CoreCLR-side owner of plugin component type registrations and all per-archetype heaps.
///
/// Lifecycle: one instance per ECS world. Call RegisterComponent&lt;T&gt; for each plugin
/// component type during initialization, pass the returned PluginComponentRegistration to the
/// AOT side, then dispose when the world shuts down.
///
/// The AOT side calls AllocHeap each time a new archetype needs a heap - no coordination
/// needed from the plugin after registration. All allocated heaps are tracked here and
/// disposed in bulk on shutdown.
/// </summary>
internal sealed class ModComponentManager : IDisposable
{
    // Keeps factory delegates alive - their IntPtrs live in AOT memory.
    private readonly PinnedDelegateStore _delegateStore = new();

    // All heaps ever allocated via any factory, for bulk disposal on shutdown.
    // Growing indefinitely is fine for a typical game session; if archetype churn becomes
    // a concern, add a per-heap dispose delegate (see note at bottom of file).
    private readonly List<IDisposable> _allHeaps = [];
    private readonly Lock _heapsLock = new();

    // -------------------------------------------------------------------------
    // Registration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Registers component type T. Returns a ModComponentInfo to hand to the
    /// AOT relay's RegisterModComponent. The embedded AllocHeap delegate allocates a
    /// fresh TypedComponentHeap&lt;T&gt; each time an archetype needs one.
    /// </summary>
    public ModComponentInfo RegisterLocalComponent<T>() where T : struct
    {
        // Bind the factory to this T at registration time. Capturing via method group
        // means each call to RegisterComponent<T> creates an independent delegate instance
        // correctly specialized for that T.
        var factory = new AllocHeapDelegate(AllocHeapImpl<T>);
        _delegateStore.PinDelegate(factory);

        return new ModComponentInfo
        {
            Stride = Unsafe.SizeOf<T>(),
            IsBlittable = IsBlittable<T>() ? (byte)1 : (byte)0,
            AllocHeap = Marshal.GetFunctionPointerForDelegate(factory),
            WriteSnapshot = IntPtr.Zero,
            ReadSnapshot = IntPtr.Zero
        };
    }

    public ModComponentInfo RegisterComponent(Type componentType)
    {
        var method = GetType().GetMethod(nameof(RegisterComponent), BindingFlags.Public | BindingFlags.Instance, []);
        Debug.Assert(method != null, "RegisterComponent method not found");

        var genericMethod = method.MakeGenericMethod(componentType);
        var result = genericMethod.Invoke(this, []) as ModComponentInfo?;
        if (result == null)
            throw new InvalidOperationException($"Failed to register component type {componentType.FullName}");

        return result.Value;
    }

    /// <summary>
    /// Registers component type T. Returns a PluginComponentRegistration to hand to the
    /// AOT relay's RegisterPluginComponent. The embedded AllocHeap delegate allocates a
    /// fresh TypedComponentHeap&lt;T&gt; each time an archetype needs one.
    /// </summary>
    public unsafe ModComponentInfo RegisterComponent<T>() where T : struct, INetworkedComponent
    {
        // Bind the factory to this T at registration time. Capturing via method group
        // means each call to RegisterComponent<T> creates an independent delegate instance
        // correctly specialized for that T.
        var allocHeapDelegate = new AllocHeapDelegate(AllocHeapImpl<T>);
        var allocHeapDelegatePtr = _delegateStore.PinDelegate(allocHeapDelegate);

        // If T is a networked component, also provide a pointer to the WriteSnapshotJob<T>.Execute method.
        // This allows the AOT side to call back into managed code to write snapshots of plugin components.

        var writer = new NetDataWriter();
        var readBuffer = new byte[1024 * 1024];
        var reader = new NetDataReader(readBuffer);

        var writeSnapshotDelegate = new WriteSnapshotDelegate((ptr, buffer, bufferSize) =>
        {
            writer.Reset();
            var data = Unsafe.AsRef<T>((void*)ptr);
            writer.Put(data);
            var bytes = writer.Data;

            if (bytes.Length > bufferSize)
                throw new InvalidOperationException($"Buffer too small for snapshot of {typeof(T).Name}: need {bytes.Length} bytes, have {bufferSize} bytes");

            Marshal.Copy(bytes, 0, (IntPtr)buffer, bytes.Length);
            return writer.Length;
        });
        var writeSnapshotDelegatePtr = _delegateStore.PinDelegate(writeSnapshotDelegate);

        var readSnapshotDelegate = new ReadSnapshotDelegate((comp, buffer, bufferSize) =>
        {
            // TODO: Replace with a span
            if (bufferSize > readBuffer.Length)
                throw new InvalidOperationException($"Buffer too small for snapshot of {typeof(T).Name}: need {bufferSize} bytes, have {readBuffer.Length} bytes");

            Marshal.Copy((IntPtr)buffer, readBuffer, 0, bufferSize);
            reader.SetPosition(0);
            var data = reader.Get<T>();
            Unsafe.Write((void*)comp, data);
            return reader.Position;
        });
        var readSnapshotDelegatePtr = _delegateStore.PinDelegate(readSnapshotDelegate);

        var writeDeltaDelegate = new WriteDeltaDelegate((ptr, buffer, bufferSize) =>
        {
            ref var data = ref Unsafe.AsRef<T>((void*)ptr);

            if (!data.IsDirty)
                return 0;

            writer.Reset();
            data.WriteDelta(writer);
            data.ClearDirty();

            var bytes = writer.Data;

            if (writer.Length > bufferSize)
                throw new InvalidOperationException($"Buffer too small for snapshot of {typeof(T).Name}: need {bytes.Length} bytes, have {bufferSize} bytes");

            Marshal.Copy(bytes, 0, (IntPtr)buffer, writer.Length);
            return writer.Length;
        });
        var writeDeltaDelegatePtr = _delegateStore.PinDelegate(writeDeltaDelegate);

        var readDeltaDelegate = new ReadDeltaDelegate((comp, buffer, bufferSize, clearDirty) =>
        {
            // TODO: Replace with a span
            if (bufferSize > readBuffer.Length)
                throw new InvalidOperationException($"Buffer too small for snapshot of {typeof(T).Name}: need {bufferSize} bytes, have {readBuffer.Length} bytes");

            Marshal.Copy((IntPtr)buffer, readBuffer, 0, bufferSize);
            reader.SetPosition(0);
            ref var data = ref Unsafe.AsRef<T>((void*)comp);
            data.ReadDelta(reader);

            if (clearDirty == 1)
            {
                data.ClearDirty();
            }

            return reader.Position;
        });
        var readDeltaDelegatePtr = _delegateStore.PinDelegate(readDeltaDelegate);

        var changedFromApiDelegate = new ChangedFromApiDelegate(ptr =>
        {
            ref var data = ref Unsafe.AsRef<T>((void*)ptr);
            return data.ChangedFromApi ? (byte)1 : (byte)0;
        });
        var changedFromApiDelegatePtr = _delegateStore.PinDelegate(changedFromApiDelegate);

        return new ModComponentInfo
        {
            Stride = Unsafe.SizeOf<T>(),
            IsBlittable = IsBlittable<T>() ? (byte)1 : (byte)0,
            AllocHeap = allocHeapDelegatePtr,
            WriteSnapshot = writeSnapshotDelegatePtr,
            ReadSnapshot = readSnapshotDelegatePtr,
            WriteDelta = writeDeltaDelegatePtr,
            ReadDelta = readDeltaDelegatePtr,
            ChangedFromApi = changedFromApiDelegatePtr
        };
    }

    // -------------------------------------------------------------------------
    // Heap factory - called from AOT via function pointer
    // -------------------------------------------------------------------------

    private void AllocHeapImpl<T>(int capacity, IntPtr outPtr) where T : struct
    {
        var heap = new TypedComponentHeap<T>(capacity);

        lock (_heapsLock)
        {
            _allHeaps.Add(heap);
        }

        // outPtr is a caller-allocated AOTHeapPointers on the AOT stack.
        // Writing through the pointer is safe: the call is synchronous and the
        // AOT stack frame that owns the struct is alive for the duration.
        unsafe
        {
            *(AOTHeapPointers*)outPtr = heap.GetPointers();
        }
    }

    private static bool IsBlittable<T>() where T : struct
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
    // Lifetime
    // -------------------------------------------------------------------------

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        // Dispose delegates first - stops any in-flight AllocHeap calls from
        // writing into heaps we are about to tear down.
        _delegateStore.Dispose();

        lock (_heapsLock)
        {
            foreach (var heap in _allHeaps)
                heap.Dispose();
            _allHeaps.Clear();
        }
    }

    ~ModComponentManager() => Dispose();
}
