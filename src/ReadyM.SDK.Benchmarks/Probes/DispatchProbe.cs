using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ReadyM.SDK.Benchmarks.Probes;

// Five types, so a loop body reaches five different heaps like the real five-component case does.
internal struct P1 { public float Value; }

internal struct P2 { public float Value; }

internal struct P3 { public float Value; }

internal struct P4 { public float Value; }

internal struct P5 { public float Value; }

/// Stand-in for the proposed <c>ComponentId&lt;T&gt;</c>: a mutable static the JIT loads but cannot fold.
internal static class ProbeId<T> where T : struct
{
    internal static int Value = -1;
}

/// Stand-in for a non-generic StructHeap base carrying the array and its stride.
internal abstract class ProbeHeap
{
    internal Array Data = null!;
    internal int Stride;
}

internal sealed class ProbeHeap<T> : ProbeHeap where T : struct
{
    internal readonly T[] Items;

    internal ProbeHeap(int count)
    {
        Items = new T[count];
        Data = Items;
        Stride = Unsafe.SizeOf<T>();
    }
}

/// Identity shaped like RawEntity: an index plus a revision that has to be checked.
internal readonly record struct ProbeEntity(int Index, short Revision);

internal interface IProbeApi
{
    /// Today's shape: generic method on an interface, returning ref T.
    ref T GetGeneric<T>(int index) where T : struct;

    /// Proposed shape: non-generic, by component id, returning ref byte.
    ref byte GetData(int index, int componentId);

    // The same two, but each call must first resolve the identity, like both halves really do.
    ref T GetGenericResolved<T>(ProbeEntity entity) where T : struct;

    ref byte GetDataResolved(ProbeEntity entity, int componentId);
}

internal sealed class ProbeApi(ProbeHeap[] heaps, short[] revisions) : IProbeApi
{
    public ref T GetGeneric<T>(int index) where T : struct
        => ref ((ProbeHeap<T>)heaps[ProbeId<T>.Value]).Items[index];

    public ref byte GetData(int index, int componentId)
    {
        var heap = heaps[componentId];
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(heap.Data), (nint)index * heap.Stride);
    }

    public ref T GetGenericResolved<T>(ProbeEntity entity) where T : struct
        => ref ((ProbeHeap<T>)heaps[ProbeId<T>.Value]).Items[Resolve(entity)];

    public ref byte GetDataResolved(ProbeEntity entity, int componentId)
    {
        var index = Resolve(entity);
        var heap = heaps[componentId];
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(heap.Data), (nint)index * heap.Stride);
    }

    /// Whether five accesses to one entity pay this once or five times is what inlining decides.
    private int Resolve(ProbeEntity entity)
    {
        if (revisions[entity.Index] != entity.Revision)
            throw new InvalidOperationException("stale identity");

        return entity.Index;
    }
}

/// Shaped like EntityHandle: one api reference plus an identity, 16 bytes.
internal readonly struct InterfaceHandle(IProbeApi api, int index)
{
    public ref T Generic<T>() where T : struct => ref api.GetGeneric<T>(index);

    public ref T NonGeneric<T>() where T : struct
        => ref Unsafe.As<byte, T>(ref api.GetData(index, ProbeId<T>.Value));

    public ref T GenericResolved<T>(ProbeEntity entity) where T : struct
        => ref api.GetGenericResolved<T>(entity);

    public ref T NonGenericResolved<T>(ProbeEntity entity) where T : struct
        => ref Unsafe.As<byte, T>(ref api.GetDataResolved(entity, ProbeId<T>.Value));
}

/// The same handle over a concrete sealed class, to separate dispatch from the work behind it.
internal readonly struct ConcreteHandle(ProbeApi api, int index)
{
    public ref T Generic<T>() where T : struct => ref api.GetGeneric<T>(index);

    public ref T NonGeneric<T>() where T : struct
        => ref Unsafe.As<byte, T>(ref api.GetData(index, ProbeId<T>.Value));

    public ref T GenericResolved<T>(ProbeEntity entity) where T : struct
        => ref api.GetGenericResolved<T>(entity);

    public ref T NonGenericResolved<T>(ProbeEntity entity) where T : struct
        => ref Unsafe.As<byte, T>(ref api.GetDataResolved(entity, ProbeId<T>.Value));
}
