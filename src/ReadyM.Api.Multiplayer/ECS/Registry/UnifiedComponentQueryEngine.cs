// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.

using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal sealed class UnifiedComponentQueryEngine(UnifiedComponentRegistry registry, EntityStore world)
{
    /// <summary>
    /// Runs a query for one component, calling <paramref name="callback"/> once per
    /// archetype chunk with a pointer to the first element, the entity count, and the stride.
    /// </summary>
    public void Query(int c1, ChunkCallback1 callback)
    {
        var e = registry.GetEntryById(c1);

        // AOT single-component: use Friflo's pre-filtered fast path.
        if (e.SingleComponentQuery != null)
        {
            e.SingleComponentQuery(world, callback);
            return;
        }

        // Plugin single-component: archetype scan.
        ScanArchetypes(
            [e.StructIndex],
            [e.Stride],
            static (callback, ptrs, count, strides) => callback(ptrs[0], count, strides[0]),
            callback);
    }

    /// <summary>Runs a query for two components. Both may be AOT, both plugin, or mixed.</summary>
    public void Query(int c1, int c2, ChunkCallback2 callback)
    {
        var e1 = registry.GetEntryById(c1);
        var e2 = registry.GetEntryById(c2);

        ScanArchetypes(
            [e1.StructIndex, e2.StructIndex],
            [e1.Stride, e2.Stride],
            static (callback, ptrs, count, strides) => callback(ptrs[0], ptrs[1], count, strides[0], strides[1]),
            callback);
    }

    /// <summary>Runs a query for three components.</summary>
    public void Query(int c1, int c2, int c3, ChunkCallback3 callback)
    {
        var e1 = registry.GetEntryById(c1);
        var e2 = registry.GetEntryById(c2);
        var e3 = registry.GetEntryById(c3);

        ScanArchetypes(
            [e1.StructIndex, e2.StructIndex, e3.StructIndex],
            [e1.Stride, e2.Stride, e3.Stride],
            static (callback, ptrs, count, strides) => callback(ptrs[0], ptrs[1], ptrs[2], count, strides[0], strides[1], strides[2]),
            callback);
    }

    /// <summary>Runs a query for four components.</summary>
    public void Query(int c1, int c2, int c3, int c4, ChunkCallback4 callback)
    {
        var e1 = registry.GetEntryById(c1);
        var e2 = registry.GetEntryById(c2);
        var e3 = registry.GetEntryById(c3);
        var e4 = registry.GetEntryById(c4);
        ScanArchetypes(
            [e1.StructIndex, e2.StructIndex, e3.StructIndex, e4.StructIndex],
            [e1.Stride, e2.Stride, e3.Stride, e4.Stride],
            static (callback, ptrs, count, strides) => callback(ptrs[0], ptrs[1], ptrs[2], ptrs[3], count, strides[0], strides[1], strides[2], strides[3]),
            callback);
    }

    /// <summary>Runs a query for five components.</summary>
    public void Query(int c1, int c2, int c3, int c4, int c5, ChunkCallback5 callback)
    {
        var e1 = registry.GetEntryById(c1);
        var e2 = registry.GetEntryById(c2);
        var e3 = registry.GetEntryById(c3);
        var e4 = registry.GetEntryById(c4);
        var e5 = registry.GetEntryById(c5);
        ScanArchetypes(
            [e1.StructIndex, e2.StructIndex, e3.StructIndex, e4.StructIndex, e5.StructIndex],
            [e1.Stride, e2.Stride, e3.Stride, e4.Stride, e5.Stride],
            static (callback, ptrs, count, strides) => callback(ptrs[0], ptrs[1], ptrs[2], ptrs[3], ptrs[4], count, strides[0], strides[1], strides[2], strides[3], strides[4]),
            callback);
    }

    /// <summary>Runs a query for six components.</summary>
    public void Query(int c1, int c2, int c3, int c4, int c5, int c6, ChunkCallback6 callback)
    {
        var e1 = registry.GetEntryById(c1);
        var e2 = registry.GetEntryById(c2);
        var e3 = registry.GetEntryById(c3);
        var e4 = registry.GetEntryById(c4);
        var e5 = registry.GetEntryById(c5);
        var e6 = registry.GetEntryById(c6);
        ScanArchetypes(
            [e1.StructIndex, e2.StructIndex, e3.StructIndex, e4.StructIndex, e5.StructIndex, e6.StructIndex],
            [e1.Stride, e2.Stride, e3.Stride, e4.Stride, e5.Stride, e6.Stride],
            static (callback, ptrs, count, strides) => callback(ptrs[0], ptrs[1], ptrs[2], ptrs[3], ptrs[4], ptrs[5], count, strides[0], strides[1], strides[2], strides[3], strides[4], strides[5]),
            callback);
    }

    private unsafe void ScanArchetypes<T>(
        ReadOnlySpan<int> structIndices,
        ReadOnlySpan<int> strides,
        SpanCallback<T> callback,
        T callbackParam)
    {
        var archetypes = world.GetArchetypes();
        var archetypeCount = world.GetArchetypeCount();
        var n = structIndices.Length;

        // Stack-allocate pointer array for the callback to avoid heap allocation per chunk.
        var ptrs = stackalloc IntPtr[n];

        for (var a = 0; a < archetypeCount; a++)
        {
            var archetype = archetypes[a];
            if (archetype == null) continue;

            var entityCount = archetype.Count;
            if (entityCount == 0) continue;

            // Check that all required components are present in this archetype.
            // Works for both AOT and plugin struct indices.
            var allPresent = true;
            for (var i = 0; i < n; i++)
            {
                if (archetype.GetHeap(structIndices[i]) == null)
                {
                    allPresent = false;
                    break;
                }
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
                    ptrs[i] = archetype.GetHeap(structIndices[i])!.ReadyMGetPtrTo(0);

                callback(callbackParam, new ReadOnlySpan<IntPtr>(ptrs, n), entityCount, strides);
            }
            finally
            {
                GC.EndNoGCRegion();
            }
        }
    }

    // Delegate for the inner callback used by ScanArchetypes.
    private delegate void SpanCallback<in T>(T callback, ReadOnlySpan<IntPtr> ptrs, int count, ReadOnlySpan<int> strides);

    // TODO: GC is active while this is being returned
    public IntPtr GetComponentPointer(int entityId, int componentType)
    {
        var entry = registry.GetEntryById(componentType);
        var entity = world.GetEntityById(entityId);
        return entity.GetComponent(entry.StructIndex);
    }
}