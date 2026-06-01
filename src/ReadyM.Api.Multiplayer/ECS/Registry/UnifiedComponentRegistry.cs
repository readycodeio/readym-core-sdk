// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

/// <summary>
/// Unified registry for all ECS component types - both AOT-compiled server components and
/// runtime-registered plugin components. All components are identified by a stable <c>int</c>
/// component ID assigned at registration time.
/// <para>
/// AOT components are registered at server startup via <see cref="RegisterComponent{T}"/>.
/// Plugin components are registered at plugin load time via <see cref="RegisterPluginComponent"/>.
/// Both kinds share the same ID space and can be mixed freely in query calls.
/// </para>
/// </summary>
internal sealed class UnifiedComponentRegistry
{
    // Chunk callback: (pointer to first element, entity count, stride in bytes).
    // Stride is always included so the recipient never needs to know sizeof(T) separately.
    public delegate void ChunkCallback(IntPtr data, int count, int stride);

    // -------------------------------------------------------------------------
    // Entry - one per registered component, AOT or plugin
    // -------------------------------------------------------------------------

    private sealed class Entry
    {
        public readonly int    ComponentId;
        public readonly bool   IsPlugin;
        public readonly Type?  ManagedType;   // null for plugin components
        public readonly int    StructIndex;   // Friflo heapMap index
        public readonly int    Stride;        // sizeof(T) or plugin stride
        public readonly DeliveryMethod DeliveryMethod;

        // AOT single-component fast path: uses world.Query<T>().ForEach() directly,
        // which is faster than archetype iteration because Friflo pre-filters archetypes.
        // null for plugin components; they always use the archetype-scan path.
        public readonly Action<ChunkCallback1>? SingleComponentQuery;

        public Entry(
            int componentId, bool isPlugin, Type? managedType,
            int structIndex, int stride, DeliveryMethod deliveryMethod,
            Action<ChunkCallback1>? singleComponentQuery)
        {
            ComponentId           = componentId;
            IsPlugin              = isPlugin;
            ManagedType           = managedType;
            StructIndex           = structIndex;
            Stride                = stride;
            DeliveryMethod        = deliveryMethod;
            SingleComponentQuery  = singleComponentQuery;
        }
    }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private readonly EntityStore _world;
    private readonly NativeAOT   _aot;        // valid only during schema build phase
    private int _nextId;

    private readonly Dictionary<int,   Entry> _byId       = new();
    private readonly Dictionary<Type,  int>   _typeToId   = new();
    private readonly Dictionary<string, int>  _nameToId   = new(); // typeof(T).FullName → id

    public UnifiedComponentRegistry(EntityStore world, NativeAOT aot)
    {
        _world = world;
        _aot   = aot;
    }

    // -------------------------------------------------------------------------
    // Registration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Registers an AOT (compile-time known) networked component. Call during server startup
    /// before <c>NativeAOT.CreateSchema()</c>.
    /// </summary>
    public int RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable)
        where T : struct, INetworkedComponent
    {
        var id          = _nextId++;
        var structIndex = StructInfo<T>.Index;
        var stride      = Unsafe.SizeOf<T>();

        // Capture world once for the fast single-component query closure.
        var world = _world;
        Action<ChunkCallback1> singleQuery = callback =>
        {
            unsafe
            {
                world.Query<T>().ForEach((chunk, _) =>
                {
                    fixed (void* buffer = chunk.Span)
                        callback((IntPtr)buffer, chunk.Span.Length, stride);
                }).Run();
            }
        };

        var entry = new Entry(id, false, typeof(T), structIndex, stride, deliveryMethod, singleQuery);
        _byId[id]              = entry;
        _typeToId[typeof(T)]   = id;
        _nameToId[typeof(T).FullName!] = id;
        return id;
    }

    /// <summary>
    /// Registers a plugin component of unknown type with a fixed stride in bytes.
    /// Call during plugin load, before <c>NativeAOT.CreateSchema()</c>.
    /// Returns the component ID the plugin must use in all subsequent query calls.
    /// </summary>
    public int RegisterPluginComponent(int stride, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable)
    {
        var id          = _nextId++;
        var structIndex = _aot.RegisterPluginComponent(stride);

        var entry = new Entry(id, true, null, structIndex, stride, deliveryMethod, null);
        _byId[id] = entry;
        return id;
    }

    // -------------------------------------------------------------------------
    // ID lookups
    // -------------------------------------------------------------------------

    public int GetId<T>()                  where T : struct => _typeToId[typeof(T)];
    public int GetId(Type type)                             => _typeToId[type];
    public int GetId(string typeFullName)                   => _nameToId[typeFullName];

    public DeliveryMethod GetDeliveryMethod(int id)  => _byId[id].DeliveryMethod;
    public Type?          GetManagedType(int id)     => _byId[id].ManagedType;

    // -------------------------------------------------------------------------
    // Query dispatch
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs a query for one component, calling <paramref name="callback"/> once per
    /// archetype chunk with a pointer to the first element, the entity count, and the stride.
    /// </summary>
    public void Query(int c1, ChunkCallback1 callback)
    {
        var e = _byId[c1];

        // AOT single-component: use Friflo's pre-filtered fast path.
        if (e.SingleComponentQuery != null)
        {
            e.SingleComponentQuery(callback);
            return;
        }

        // Plugin single-component: archetype scan.
        ScanArchetypes(stackalloc int[] { e.StructIndex },
                       stackalloc int[] { e.Stride },
                       (ptrs, count, strides) => callback(ptrs[0], count, strides[0]));
    }

    /// <summary>Runs a query for two components. Both may be AOT, both plugin, or mixed.</summary>
    public void Query(int c1, int c2, ChunkCallback2 callback)
    {
        var e1 = _byId[c1];
        var e2 = _byId[c2];
        ScanArchetypes(
            stackalloc int[] { e1.StructIndex, e2.StructIndex },
            stackalloc int[] { e1.Stride,      e2.Stride },
            (ptrs, count, strides) => callback(ptrs[0], ptrs[1], count, strides[0], strides[1]));
    }

    /// <summary>Runs a query for three components.</summary>
    public void Query(int c1, int c2, int c3, ChunkCallback3 callback)
    {
        var e1 = _byId[c1]; var e2 = _byId[c2]; var e3 = _byId[c3];
        ScanArchetypes(
            stackalloc int[] { e1.StructIndex, e2.StructIndex, e3.StructIndex },
            stackalloc int[] { e1.Stride,      e2.Stride,      e3.Stride },
            (ptrs, count, strides) => callback(ptrs[0], ptrs[1], ptrs[2], count, strides[0], strides[1], strides[2]));
    }

    /// <summary>Runs a query for four components.</summary>
    public void Query(int c1, int c2, int c3, int c4, ChunkCallback4 callback)
    {
        var e1 = _byId[c1]; var e2 = _byId[c2];
        var e3 = _byId[c3]; var e4 = _byId[c4];
        ScanArchetypes(
            stackalloc int[] { e1.StructIndex, e2.StructIndex, e3.StructIndex, e4.StructIndex },
            stackalloc int[] { e1.Stride,      e2.Stride,      e3.Stride,      e4.Stride },
            (ptrs, count, strides) => callback(ptrs[0], ptrs[1], ptrs[2], ptrs[3],
                                               count, strides[0], strides[1], strides[2], strides[3]));
    }

    /// <summary>Runs a query for five components.</summary>
    public void Query(int c1, int c2, int c3, int c4, int c5, ChunkCallback5 callback)
    {
        var e1 = _byId[c1]; var e2 = _byId[c2]; var e3 = _byId[c3];
        var e4 = _byId[c4]; var e5 = _byId[c5];
        ScanArchetypes(
            stackalloc int[] { e1.StructIndex, e2.StructIndex, e3.StructIndex, e4.StructIndex, e5.StructIndex },
            stackalloc int[] { e1.Stride,      e2.Stride,      e3.Stride,      e4.Stride,      e5.Stride },
            (ptrs, count, strides) => callback(ptrs[0], ptrs[1], ptrs[2], ptrs[3], ptrs[4],
                                               count, strides[0], strides[1], strides[2], strides[3], strides[4]));
    }

    /// <summary>Runs a query for six components.</summary>
    public void Query(int c1, int c2, int c3, int c4, int c5, int c6, ChunkCallback6 callback)
    {
        var e1 = _byId[c1]; var e2 = _byId[c2]; var e3 = _byId[c3];
        var e4 = _byId[c4]; var e5 = _byId[c5]; var e6 = _byId[c6];
        ScanArchetypes(
            stackalloc int[] { e1.StructIndex, e2.StructIndex, e3.StructIndex, e4.StructIndex, e5.StructIndex, e6.StructIndex },
            stackalloc int[] { e1.Stride,      e2.Stride,      e3.Stride,      e4.Stride,      e5.Stride,      e6.Stride },
            (ptrs, count, strides) => callback(ptrs[0], ptrs[1], ptrs[2], ptrs[3], ptrs[4], ptrs[5],
                                               count, strides[0], strides[1], strides[2], strides[3], strides[4], strides[5]));
    }

    // -------------------------------------------------------------------------
    // Core archetype scan - the unified dispatch path for N components.
    //
    // Works for any combination of AOT and plugin components because
    // StructHeap.ReadyMGetPtrToFirst() is virtual and correct for both
    // StructHeap<T> and PluginStructHeap. The stride stored in the entry
    // accounts for the per-element size in either case.
    //
    // For single AOT queries we skip this in favour of world.Query<T>().ForEach()
    // which is faster (Friflo pre-filters archetypes). For multi-component queries
    // the archetype scan is equivalent to what the existing RunQuery2 did.
    // -------------------------------------------------------------------------

    private unsafe void ScanArchetypes(
        ReadOnlySpan<int> structIndices,
        ReadOnlySpan<int> strides,
        SpanCallback callback)
    {
        var archetypes     = _world.GetArchetypes();
        var archetypeCount = _world.GetArchetypeCount();
        var n              = structIndices.Length;

        // Stack-allocate pointer array for the callback to avoid heap allocation per chunk.
        var ptrs = stackalloc IntPtr[n];

        for (var a = 0; a < archetypeCount; a++)
        {
            var archetype = archetypes[a];
            if (archetype == null) continue;

            var entityCount = archetype.EntityCount;
            if (entityCount == 0) continue;

            // Check that all required components are present in this archetype.
            // Works for both AOT and plugin struct indices.
            var allPresent = true;
            for (var i = 0; i < n; i++)
            {
                if (archetype.GetHeap(structIndices[i]) == null) { allPresent = false; break; }
            }
            if (!allPresent) continue;

            // Disable GC for the duration of the pointer window.
            // ReadyMGetPtrToFirst() is only safe when the GC cannot move arrays.
            var inNoGcRegion = GC.TryStartNoGCRegion(1 * 1024 * 1024, true);
            if (!inNoGcRegion)
                throw new InvalidOperationException(
                    "Failed to start no-GC region for ECS query. " +
                    "Increase the reserved size or reduce concurrent GC pressure.");

            try
            {
                for (var i = 0; i < n; i++)
                    ptrs[i] = archetype.GetHeap(structIndices[i])!.ReadyMGetPtrToFirst();

                callback(new ReadOnlySpan<IntPtr>(ptrs, n), entityCount, strides);
            }
            finally
            {
                GC.EndNoGCRegion();
            }
        }
    }

    // Delegate for the inner callback used by ScanArchetypes.
    private delegate void SpanCallback(ReadOnlySpan<IntPtr> ptrs, int count, ReadOnlySpan<int> strides);
}