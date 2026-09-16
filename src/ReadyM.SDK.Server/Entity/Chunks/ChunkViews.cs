using System.ComponentModel;
using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity.Chunks;

/// <summary>2 shapes over one chunk, so a query can ask for all of them without an archetype naming them.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct ChunkViews<T1, T2> : IArchetypeChunkView<ChunkViews<T1, T2>>
    where T1 : IArchetypeChunkView<T1>, allows ref struct
    where T2 : IArchetypeChunkView<T2>, allows ref struct
{
    private readonly T1 _v1;
    private readonly T2 _v2;

    private ChunkViews(T1 v1, T2 v2)
    {
        _v1 = v1;
        _v2 = v2;
    }

    public static int SlotCount => T1.SlotCount + T2.SlotCount;

    // Combined once per instantiation rather than per query: a single shape reads its set off a
    // cached static, and a composed one has to match that or it allocates on every loop.
    private static readonly ComponentSet Combined = ComponentSet.Combine(T1.Components, T2.Components);

    public static ComponentSet Components => Combined;

    /// Each view takes the slots after the ones before it, by the count each reports.
    public static ChunkViews<T1, T2> Bind(
        ReadOnlySpan<ChunkSlot> slots,
        ReadOnlySpan<RawEntity> entities,
        EntityHandle prototype)
    {
        var end1 = T1.SlotCount;

        return new(
            T1.Bind(slots[..end1], entities, prototype),
            T2.Bind(slots[end1..], entities, prototype));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkViews<T1, T2> At(int index) => new(_v1.At(index), _v2.At(index));

    /// <summary>What lets a loop read <c>foreach (var (first, second) in ...)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T1 first, out T2 second)
    {
        first = _v1;
        second = _v2;
    }
}
/// <summary>3 shapes over one chunk, so a query can ask for all of them without an archetype naming them.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct ChunkViews<T1, T2, T3> : IArchetypeChunkView<ChunkViews<T1, T2, T3>>
    where T1 : IArchetypeChunkView<T1>, allows ref struct
    where T2 : IArchetypeChunkView<T2>, allows ref struct
    where T3 : IArchetypeChunkView<T3>, allows ref struct
{
    private readonly T1 _v1;
    private readonly T2 _v2;
    private readonly T3 _v3;

    private ChunkViews(T1 v1, T2 v2, T3 v3)
    {
        _v1 = v1;
        _v2 = v2;
        _v3 = v3;
    }

    public static int SlotCount => T1.SlotCount + T2.SlotCount + T3.SlotCount;

    // Combined once per instantiation rather than per query: a single shape reads its set off a
    // cached static, and a composed one has to match that or it allocates on every loop.
    private static readonly ComponentSet Combined = ComponentSet.Combine(T1.Components, T2.Components, T3.Components);

    public static ComponentSet Components => Combined;

    /// Each view takes the slots after the ones before it, by the count each reports.
    public static ChunkViews<T1, T2, T3> Bind(
        ReadOnlySpan<ChunkSlot> slots,
        ReadOnlySpan<RawEntity> entities,
        EntityHandle prototype)
    {
        var end1 = T1.SlotCount;
        var end2 = end1 + T2.SlotCount;

        return new(
            T1.Bind(slots[..end1], entities, prototype),
            T2.Bind(slots[end1..end2], entities, prototype),
            T3.Bind(slots[end2..], entities, prototype));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkViews<T1, T2, T3> At(int index) => new(_v1.At(index), _v2.At(index), _v3.At(index));

    /// <summary>What lets a loop read <c>foreach (var (first, second, third) in ...)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T1 first, out T2 second, out T3 third)
    {
        first = _v1;
        second = _v2;
        third = _v3;
    }
}
/// <summary>4 shapes over one chunk, so a query can ask for all of them without an archetype naming them.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct ChunkViews<T1, T2, T3, T4> : IArchetypeChunkView<ChunkViews<T1, T2, T3, T4>>
    where T1 : IArchetypeChunkView<T1>, allows ref struct
    where T2 : IArchetypeChunkView<T2>, allows ref struct
    where T3 : IArchetypeChunkView<T3>, allows ref struct
    where T4 : IArchetypeChunkView<T4>, allows ref struct
{
    private readonly T1 _v1;
    private readonly T2 _v2;
    private readonly T3 _v3;
    private readonly T4 _v4;

    private ChunkViews(T1 v1, T2 v2, T3 v3, T4 v4)
    {
        _v1 = v1;
        _v2 = v2;
        _v3 = v3;
        _v4 = v4;
    }

    public static int SlotCount => T1.SlotCount + T2.SlotCount + T3.SlotCount + T4.SlotCount;

    // Combined once per instantiation rather than per query: a single shape reads its set off a
    // cached static, and a composed one has to match that or it allocates on every loop.
    private static readonly ComponentSet Combined = ComponentSet.Combine(T1.Components, T2.Components, T3.Components, T4.Components);

    public static ComponentSet Components => Combined;

    /// Each view takes the slots after the ones before it, by the count each reports.
    public static ChunkViews<T1, T2, T3, T4> Bind(
        ReadOnlySpan<ChunkSlot> slots,
        ReadOnlySpan<RawEntity> entities,
        EntityHandle prototype)
    {
        var end1 = T1.SlotCount;
        var end2 = end1 + T2.SlotCount;
        var end3 = end2 + T3.SlotCount;

        return new(
            T1.Bind(slots[..end1], entities, prototype),
            T2.Bind(slots[end1..end2], entities, prototype),
            T3.Bind(slots[end2..end3], entities, prototype),
            T4.Bind(slots[end3..], entities, prototype));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkViews<T1, T2, T3, T4> At(int index) => new(_v1.At(index), _v2.At(index), _v3.At(index), _v4.At(index));

    /// <summary>What lets a loop read <c>foreach (var (first, second, third, fourth) in ...)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T1 first, out T2 second, out T3 third, out T4 fourth)
    {
        first = _v1;
        second = _v2;
        third = _v3;
        fourth = _v4;
    }
}
/// <summary>5 shapes over one chunk, so a query can ask for all of them without an archetype naming them.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct ChunkViews<T1, T2, T3, T4, T5> : IArchetypeChunkView<ChunkViews<T1, T2, T3, T4, T5>>
    where T1 : IArchetypeChunkView<T1>, allows ref struct
    where T2 : IArchetypeChunkView<T2>, allows ref struct
    where T3 : IArchetypeChunkView<T3>, allows ref struct
    where T4 : IArchetypeChunkView<T4>, allows ref struct
    where T5 : IArchetypeChunkView<T5>, allows ref struct
{
    private readonly T1 _v1;
    private readonly T2 _v2;
    private readonly T3 _v3;
    private readonly T4 _v4;
    private readonly T5 _v5;

    private ChunkViews(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5)
    {
        _v1 = v1;
        _v2 = v2;
        _v3 = v3;
        _v4 = v4;
        _v5 = v5;
    }

    public static int SlotCount => T1.SlotCount + T2.SlotCount + T3.SlotCount + T4.SlotCount + T5.SlotCount;

    // Combined once per instantiation rather than per query: a single shape reads its set off a
    // cached static, and a composed one has to match that or it allocates on every loop.
    private static readonly ComponentSet Combined = ComponentSet.Combine(T1.Components, T2.Components, T3.Components, T4.Components, T5.Components);

    public static ComponentSet Components => Combined;

    /// Each view takes the slots after the ones before it, by the count each reports.
    public static ChunkViews<T1, T2, T3, T4, T5> Bind(
        ReadOnlySpan<ChunkSlot> slots,
        ReadOnlySpan<RawEntity> entities,
        EntityHandle prototype)
    {
        var end1 = T1.SlotCount;
        var end2 = end1 + T2.SlotCount;
        var end3 = end2 + T3.SlotCount;
        var end4 = end3 + T4.SlotCount;

        return new(
            T1.Bind(slots[..end1], entities, prototype),
            T2.Bind(slots[end1..end2], entities, prototype),
            T3.Bind(slots[end2..end3], entities, prototype),
            T4.Bind(slots[end3..end4], entities, prototype),
            T5.Bind(slots[end4..], entities, prototype));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkViews<T1, T2, T3, T4, T5> At(int index) => new(_v1.At(index), _v2.At(index), _v3.At(index), _v4.At(index), _v5.At(index));

    /// <summary>What lets a loop read <c>foreach (var (first, second, third, fourth, fifth) in ...)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T1 first, out T2 second, out T3 third, out T4 fourth, out T5 fifth)
    {
        first = _v1;
        second = _v2;
        third = _v3;
        fourth = _v4;
        fifth = _v5;
    }
}
/// <summary>6 shapes over one chunk, so a query can ask for all of them without an archetype naming them.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct ChunkViews<T1, T2, T3, T4, T5, T6> : IArchetypeChunkView<ChunkViews<T1, T2, T3, T4, T5, T6>>
    where T1 : IArchetypeChunkView<T1>, allows ref struct
    where T2 : IArchetypeChunkView<T2>, allows ref struct
    where T3 : IArchetypeChunkView<T3>, allows ref struct
    where T4 : IArchetypeChunkView<T4>, allows ref struct
    where T5 : IArchetypeChunkView<T5>, allows ref struct
    where T6 : IArchetypeChunkView<T6>, allows ref struct
{
    private readonly T1 _v1;
    private readonly T2 _v2;
    private readonly T3 _v3;
    private readonly T4 _v4;
    private readonly T5 _v5;
    private readonly T6 _v6;

    private ChunkViews(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6)
    {
        _v1 = v1;
        _v2 = v2;
        _v3 = v3;
        _v4 = v4;
        _v5 = v5;
        _v6 = v6;
    }

    public static int SlotCount => T1.SlotCount + T2.SlotCount + T3.SlotCount + T4.SlotCount + T5.SlotCount + T6.SlotCount;

    // Combined once per instantiation rather than per query: a single shape reads its set off a
    // cached static, and a composed one has to match that or it allocates on every loop.
    private static readonly ComponentSet Combined = ComponentSet.Combine(T1.Components, T2.Components, T3.Components, T4.Components, T5.Components, T6.Components);

    public static ComponentSet Components => Combined;

    /// Each view takes the slots after the ones before it, by the count each reports.
    public static ChunkViews<T1, T2, T3, T4, T5, T6> Bind(
        ReadOnlySpan<ChunkSlot> slots,
        ReadOnlySpan<RawEntity> entities,
        EntityHandle prototype)
    {
        var end1 = T1.SlotCount;
        var end2 = end1 + T2.SlotCount;
        var end3 = end2 + T3.SlotCount;
        var end4 = end3 + T4.SlotCount;
        var end5 = end4 + T5.SlotCount;

        return new(
            T1.Bind(slots[..end1], entities, prototype),
            T2.Bind(slots[end1..end2], entities, prototype),
            T3.Bind(slots[end2..end3], entities, prototype),
            T4.Bind(slots[end3..end4], entities, prototype),
            T5.Bind(slots[end4..end5], entities, prototype),
            T6.Bind(slots[end5..], entities, prototype));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkViews<T1, T2, T3, T4, T5, T6> At(int index) => new(_v1.At(index), _v2.At(index), _v3.At(index), _v4.At(index), _v5.At(index), _v6.At(index));

    /// <summary>What lets a loop read <c>foreach (var (first, second, third, fourth, fifth, sixth) in ...)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T1 first, out T2 second, out T3 third, out T4 fourth, out T5 fifth, out T6 sixth)
    {
        first = _v1;
        second = _v2;
        third = _v3;
        fourth = _v4;
        fifth = _v5;
        sixth = _v6;
    }
}
