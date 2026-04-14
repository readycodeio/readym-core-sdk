using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential)]
public struct NativeDictionary<TKey, TValue, THash>(int initialCapacity, AllocatorKind kind)
    : IDisposable, IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : unmanaged
    where TValue : unmanaged
    where THash : struct, IHashFunction<TKey>
{
    private NativeHashCollection<TKey, TValue> _impl = new(initialCapacity, kind);

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

    public void Dispose()
    {
        _impl.Dispose();
    }

    public bool IsCreated
        => _impl.IsCreated;

    public int Count
        => _impl.Count;

    public int Capacity
        => _impl.Capacity;

    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var hash = default(THash).ComputeHash(key);
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
            var hash = default(THash).ComputeHash(key);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        if (!IsCreated)
        {
            throw new InvalidOperationException("NativeDictionary is not created");
        }

        return new Enumerator(_impl);
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public ref TValue GetItemRef(TKey key)
    {
        var hash = default(THash).ComputeHash(key);
        var entryPtr = _impl.Find(key, hash);
        if (entryPtr.IsNull)
        {
            throw new InvalidOperationException("Key not found");
        }
        
        return ref entryPtr.Get().Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(TKey key, TValue value)
    {
        var hash = default(THash).ComputeHash(key);
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
    public bool Add(KeyValuePair<TKey, TValue> item)
        => Add(item.Key, item.Value);

    public void Clear()
        => _impl.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (TryGetValue(item.Key, out TValue value))
        {
            return EqualityComparer<TValue>.Default.Equals(item.Value, value);
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey key, TValue value)
    {
        if (TryGetValue(key, out TValue innerValue))
        {
            return EqualityComparer<TValue>.Default.Equals(value, innerValue);
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
        => !_impl.Find(key, default(THash).ComputeHash(key)).IsNull;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
        => _impl.Remove(key, default(THash).ComputeHash(key));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        var entry = _impl.Find(key, default(THash).ComputeHash(key));
        if (!entry.IsNull)
        {
            value = entry.Get().Value;
            return true;
        }
        value = default;
        return false;
    }
}