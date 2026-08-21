using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential)]
public struct NativeDictionary<TKey, TValue, THash> : IDisposable, IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : unmanaged
    where TValue : unmanaged
    where THash : struct, IHashFunction<TKey>
{
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private NativeHashCollection<TKey, TValue>.Enumerator _implEnumerator;

        internal Enumerator(NativeHashCollection<TKey, TValue> impl)
        {
            _implEnumerator = impl.GetEnumerator();
        }

        public KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var entry = _implEnumerator.Current;
                return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }

        object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
            => _implEnumerator.MoveNext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
            => _implEnumerator.Reset();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
            => _implEnumerator.Dispose();
    }

    public readonly ref struct ReadOnly(NativeDictionary<TKey, TValue, THash> owner) : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        internal readonly NativeDictionary<TKey, TValue, THash> _impl = owner;

        public AllocatorKind Allocator
            => _impl.Allocator;

        [Pure]
        public bool IsCreated
            => _impl.IsCreated;

        [Pure]
        public int Count
            => _impl.Count;

        [Pure]
        public int Capacity
            => _impl.Capacity;

        [Pure]
        public TValue this[in TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _impl[key];
        }

        // -- read access

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(in TKey key)
            => _impl.ContainsKey(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in KeyValuePair<TKey, TValue> item)
            => _impl.Contains(item);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in TKey key, TValue value)
            => _impl.Contains(key, value);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TComparer>(in KeyValuePair<TKey, TValue> item, TComparer comparer)
            where TComparer : IEqualityComparer<TValue>
            => _impl.Contains(item, comparer);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TComparer>(in TKey key, TValue value, TComparer comparer)
            where TComparer : IEqualityComparer<TValue>
        {
            if (TryGetValue(key, out TValue innerValue))
            {
                return comparer.Equals(value, innerValue);
            }
            return false;
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(in TKey key, out TValue value)
            => _impl.TryGetValue(key, out value);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(in NativeDictionary<TKey, TValue, THash> other)
            => _impl.Equals(other);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(in ReadOnly other)
            => _impl.Equals(other);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals<TComparer>(in NativeDictionary<TKey, TValue, THash> other, TComparer comparer)
            where TComparer : IEqualityComparer<TValue>
            => _impl.Equals(other, comparer);

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals<TComparer>(in ReadOnly other, TComparer comparer)
            where TComparer : IEqualityComparer<TValue>
            => _impl.Equals(other, comparer);

        // -- access object

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => _impl.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    private NativeHashCollection<TKey, TValue> _impl;
    private NativeTracker _tracker;

    private readonly void EnsureCreated()
    {
        if (!_impl.IsCreated)
            throw new InvalidOperationException("NativeDictionary is not created");
    }

    // ReSharper disable once ConvertToPrimaryConstructor
    public NativeDictionary(int initialCapacity, AllocatorKind kind)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

        _impl = new NativeHashCollection<TKey, TValue>(initialCapacity, kind);
        _tracker = NativeTracker.Alloc();
    }

    public void Dispose()
    {
        _tracker.Free();
        _impl.Dispose();
    }

    public void TryCreate(AllocatorKind kind)
    {
        if (_impl.IsCreated)
        {
            _tracker.Check();
            return;
        }
        _tracker = NativeTracker.Alloc();
        _impl = new NativeHashCollection<TKey, TValue>(0, kind);
    }

    [Pure]
    public readonly AllocatorKind Allocator
        => _impl.Allocator;

    [Pure]
    public readonly bool IsCreated
        => _impl.IsCreated;

    [Pure]
    public readonly int Count
    {
        get
        {
            _tracker.Check();
            EnsureCreated();
            return _impl.Count;
        }
    }

    [Pure]
    public readonly int Capacity
    {
        get
        {
            _tracker.Check();
            EnsureCreated();
            return _impl.Capacity;
        }
    }

    public TValue this[in TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            _tracker.Check();
            EnsureCreated();

            var hash = default(THash).ComputeHash(in key);
            var entryPtr = _impl.Find(key, hash);
            if (entryPtr.IsNull)
            {
                throw new InvalidOperationException("Key not found");
            }
            return entryPtr.Get().Value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _tracker.MarkChange();
            EnsureCreated();

            var hash = default(THash).ComputeHash(in key);
            var entryPtr = _impl.Find(key, hash);
            if (!entryPtr.IsNull)
            {
                entryPtr.Get().Value = value;
            }
            else
            {
                _impl.Insert(key, hash, value);
            }
        }
    }

    // -- read access

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ContainsKey(in TKey key)
    {
        _tracker.Check();
        EnsureCreated();
        return !_impl.Find(key, default(THash).ComputeHash(in key)).IsNull;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(in KeyValuePair<TKey, TValue> item)
    {
        _tracker.Check();
        EnsureCreated();

        if (TryGetValue(item.Key, out TValue value))
        {
            return EqualityComparer<TValue>.Default.Equals(item.Value, value);
        }
        return false;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(in TKey key, TValue value)
    {
        _tracker.Check();
        EnsureCreated();

        if (TryGetValue(key, out TValue innerValue))
        {
            return EqualityComparer<TValue>.Default.Equals(value, innerValue);
        }
        return false;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains<TComparer>(in KeyValuePair<TKey, TValue> item, TComparer comparer)
        where TComparer : IEqualityComparer<TValue>
    {
        _tracker.Check();
        EnsureCreated();

        if (TryGetValue(item.Key, out TValue value))
        {
            return comparer.Equals(item.Value, value);
        }
        return false;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains<TComparer>(in TKey key, TValue value, TComparer comparer)
        where TComparer : IEqualityComparer<TValue>
    {
        _tracker.Check();
        EnsureCreated();

        if (TryGetValue(key, out TValue innerValue))
        {
            return comparer.Equals(value, innerValue);
        }
        return false;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetValue(in TKey key, out TValue value)
    {
        _tracker.Check();
        EnsureCreated();

        var entry = _impl.Find(key, default(THash).ComputeHash(in key));
        if (!entry.IsNull)
        {
            value = entry.Get().Value;
            return true;
        }
        value = default;
        return false;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(in NativeDictionary<TKey, TValue, THash> other)
    {
        _tracker.Check();
        other._tracker.Check();

        if (_impl.GetRawBucketsPointer() == other._impl.GetRawBucketsPointer())
            return true; // Reference equality short circuit

        if (_impl.IsCreated != other._impl.IsCreated)
            return false;

        if (!_impl.IsCreated)
            return true;

        if (Count != other.Count)
            return false;

        foreach (var item in other)
        {
            if (!Contains(item.Key, item.Value))
                return false;
        }

        return true;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool Equals(in ReadOnly other)
        => Equals(other._impl);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals<TComparer>(in NativeDictionary<TKey, TValue, THash> other, TComparer comparer)
        where TComparer : IEqualityComparer<TValue>
    {
        _tracker.Check();
        other._tracker.Check();

        if (_impl.GetRawBucketsPointer() == other._impl.GetRawBucketsPointer())
            return true; // Reference equality short circuit

        if (_impl.IsCreated != other._impl.IsCreated)
            return false;

        if (!_impl.IsCreated)
            return true;

        if (Count != other.Count)
            return false;

        foreach (var item in other)
        {
            if (!Contains(item.Key, item.Value, comparer))
                return false;
        }

        return true;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Equals(in ReadOnly other, IEqualityComparer<TValue> comparer)
        => Equals(other._impl, comparer);

    // -- write access

    public ref TValue GetItemRef(in TKey key)
    {
        _tracker.Check();
        EnsureCreated();

        var hash = default(THash).ComputeHash(in key);
        var entryPtr = _impl.Find(key, hash);
        if (entryPtr.IsNull)
        {
            throw new InvalidOperationException($"Key not found: {key}");
        }

        return ref entryPtr.Get().Value;
    }

    public ref TValue GetItemRef(TKey key)
    {
        _tracker.Check();
        EnsureCreated();

        var hash = default(THash).ComputeHash(in key);
        var entryPtr = _impl.Find(key, hash);
        if (entryPtr.IsNull)
        {
            throw new InvalidOperationException($"Key not found: {key}");
        }

        return ref entryPtr.Get().Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(in TKey key, TValue value)
    {
        _tracker.MarkChange();
        EnsureCreated();

        var hash = default(THash).ComputeHash(in key);
        var entryPtr = _impl.Find(key, hash);
        if (!entryPtr.IsNull)
        {
            return false;
        }
        else
        {
            _impl.Insert(key, hash, value);
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySet(in TKey key, TValue value)
    {
        _tracker.MarkChange();
        EnsureCreated();

        var hash = default(THash).ComputeHash(in key);
        var entryPtr = _impl.Find(key, hash);
        if (!entryPtr.IsNull)
        {
            if (EqualityComparer<TValue>.Default.Equals(entryPtr.Get().Value, value))
                return false;

            entryPtr.Get().Value = value;
            return true;
        }
        else
        {
            _impl.Insert(key, hash, value);
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(in TKey key, TValue value)
    {
        _tracker.MarkChange();
        EnsureCreated();

        var hash = default(THash).ComputeHash(in key);
        var entryPtr = _impl.Find(key, hash);
        if (!entryPtr.IsNull)
        {
            entryPtr.Get().Value = value;
        }
        else
        {
            _impl.Insert(key, hash, value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(in KeyValuePair<TKey, TValue> item)
        => Add(item.Key, item.Value);

    public void Clear()
    {
        _tracker.MarkChange();
        EnsureCreated();
        _impl.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(in TKey key)
    {
        _tracker.MarkChange();
        EnsureCreated();
        return _impl.Remove(key, default(THash).ComputeHash(in key));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Assign(in NativeDictionary<TKey, TValue, THash> other)
    {
        _tracker.Check();
        other._tracker.Check();

        if (other._impl.GetRawBucketsPointer() == _impl.GetRawBucketsPointer())
            return;

        EnsureCreated();

        if (!other._impl.IsCreated)
            throw new InvalidOperationException("Source NativeDictionary is not created");

        _tracker.MarkChangeNoCheck();

        Clear();
        foreach (var item in other)
        {
            Add(item.Key, item.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Assign(ReadOnly other)
        => Assign(other._impl);

    // -- access object

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator()
    {
        _tracker.Check();
        EnsureCreated();
        return new Enumerator(_impl);
    }

    readonly IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => GetEnumerator();

    readonly IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    [Pure]
    public readonly ReadOnly AsReadOnly()
    {
        _tracker.Check();
        EnsureCreated();
        return new ReadOnly(this);
    }

    public void Check()
        => _tracker.Check();
}
