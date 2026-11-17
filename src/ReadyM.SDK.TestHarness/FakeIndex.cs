using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;

namespace ReadyM.SDK.TestHarness;

/// <summary>The index the relay keeps for a component it owns, as the harness has to model it.</summary>
internal abstract class FakeIndex
{
    internal abstract object Read(IntPtr data);

    internal abstract void Remember(object component, RawEntity entity);

    internal abstract void Forget(object component, RawEntity entity);

    internal abstract bool TryFind(IntPtr value, out RawEntity entity);

    /// <summary>Every holder of the value, newest first: one per dimension, the way the relay's index can hold them.</summary>
    internal abstract IReadOnlyList<RawEntity> Holders(IntPtr value);
}

internal sealed class FakeIndex<T, TValue> : FakeIndex
    where T : struct, IIndexedComponent<TValue>
    where TValue : unmanaged
{
    private readonly Dictionary<TValue, List<RawEntity>> _byValue = new();

    internal override unsafe object Read(IntPtr data) => Unsafe.Read<T>((void*)data);

    internal override void Remember(object component, RawEntity entity)
    {
        var key = ((T)component).GetIndexedValue();

        if (!_byValue.TryGetValue(key, out var holders))
            _byValue[key] = holders = [];

        // Newest first: a lookup that takes one takes the latest writer, as a single-slot index did
        holders.Remove(entity);
        holders.Insert(0, entity);
    }

    internal override void Forget(object component, RawEntity entity)
    {
        var key = ((T)component).GetIndexedValue();

        if (_byValue.TryGetValue(key, out var holders) && holders.Remove(entity) && holders.Count == 0)
            _byValue.Remove(key);
    }

    internal override bool TryFind(IntPtr value, out RawEntity entity)
    {
        var holders = Holders(value);
        entity = holders.Count > 0 ? holders[0] : default;
        return holders.Count > 0;
    }

    internal override unsafe IReadOnlyList<RawEntity> Holders(IntPtr value)
        => _byValue.TryGetValue(Unsafe.Read<TValue>((void*)value), out var holders) ? holders : [];
}
