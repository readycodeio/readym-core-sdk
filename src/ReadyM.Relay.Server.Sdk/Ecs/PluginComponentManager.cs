using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
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
public sealed class PluginComponentManager : IDisposable
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
    /// Registers component type T. Returns a PluginComponentRegistration to hand to the
    /// AOT relay's RegisterPluginComponent. The embedded AllocHeap delegate allocates a
    /// fresh TypedComponentHeap&lt;T&gt; each time an archetype needs one.
    /// </summary>
    public PluginComponentRegistration RegisterComponent<T>() where T : struct
    {
        // Bind the factory to this T at registration time. Capturing via method group
        // means each call to RegisterComponent<T> creates an independent delegate instance
        // correctly specialized for that T.
        var factory = new AllocHeapDelegate(AllocHeapImpl<T>);
        _delegateStore.PinDelegate(factory);

        return new PluginComponentRegistration
        {
            Stride      = Unsafe.SizeOf<T>(),
            IsBlittable = IsBlittable<T>() ? (byte)1 : (byte)0,
            AllocHeap   = Marshal.GetFunctionPointerForDelegate(factory),
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
        catch { return false; }
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

    ~PluginComponentManager() => Dispose();
}