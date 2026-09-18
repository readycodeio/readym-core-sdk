using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;

namespace ReadyM.SDK.TestHarness;

/// The index the relay keeps for a component it owns, as the harness has to model it.
internal abstract class FakeIndex
{
    internal abstract object Read(IntPtr data);

    internal abstract void Remember(object component, RawEntity entity);

    internal abstract void Forget(object component, RawEntity entity);

    internal abstract bool TryFind(IntPtr value, out RawEntity entity);
}

internal sealed class FakeIndex<T, TValue> : FakeIndex
    where T : struct, IIndexedComponent<TValue>
    where TValue : unmanaged
{
    private readonly Dictionary<TValue, RawEntity> _byValue = new();

    internal override unsafe object Read(IntPtr data) => Unsafe.Read<T>((void*)data);

    internal override void Remember(object component, RawEntity entity)
        => _byValue[((T)component).GetIndexedValue()] = entity;

    internal override void Forget(object component, RawEntity entity)
    {
        var key = ((T)component).GetIndexedValue();

        if (_byValue.TryGetValue(key, out var found) && found == entity)
            _byValue.Remove(key);
    }

    internal override unsafe bool TryFind(IntPtr value, out RawEntity entity)
        => _byValue.TryGetValue(Unsafe.Read<TValue>((void*)value), out entity);
}
