using System;
using System.Collections;
using System.Collections.Generic;
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

    private NativeHashCollection<TKey, TValue> _impl;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NativeDictionary(int initialCapacity, AllocatorKind kind)
    {
        _impl = new NativeHashCollection<TKey, TValue>(initialCapacity, kind);
    }

    public void Dispose()
    {
        _impl.Dispose();
    }

    public void TryCreate(AllocatorKind kind)
    {
        if (_impl.IsCreated)
            return;
        _impl = new NativeHashCollection<TKey, TValue>(0, kind);
    }
    
    public bool IsCreated
        => _impl.IsCreated;

    public int Count
        => _impl.Count;

    public int Capacity
        => _impl.Capacity;

    public TValue this[in TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(in TKey key)
        => !_impl.Find(key, default(THash).ComputeHash(in key)).IsNull;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(in KeyValuePair<TKey, TValue> item)
    {
        if (TryGetValue(item.Key, out TValue value))
        {
            return EqualityComparer<TValue>.Default.Equals(item.Value, value);
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(in TKey key, TValue value)
    {
        if (TryGetValue(key, out TValue innerValue))
        {
            return EqualityComparer<TValue>.Default.Equals(value, innerValue);
        }
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<TComparer>(in KeyValuePair<TKey, TValue> item, TComparer comparer)
        where TComparer : IEqualityComparer<TValue>
    {
        if (TryGetValue(item.Key, out TValue value))
        {
            return comparer.Equals(item.Value, value);
        }
        return false;
    }
    
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(in TKey key, out TValue value)
    {
        var entry = _impl.Find(key, default(THash).ComputeHash(in key));
        if (!entry.IsNull)
        {
            value = entry.Get().Value;
            return true;
        }
        value = default;
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(in NativeDictionary<TKey, TValue, THash> other)
    {
        if (_impl.GetRawBucketsPointer() == other._impl.GetRawBucketsPointer())
            return true; // Reference equality short circuit
        
        if (_impl.IsCreated != other._impl.IsCreated)
            return false;
        
        if (Count != other.Count)
            return false;

        foreach (var item in other)
        {
            if (!Contains(item.Key, item.Value))
                return false;
        }
        
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals<TComparer>(in NativeDictionary<TKey, TValue, THash> other, TComparer comparer)
        where TComparer : IEqualityComparer<TValue>
    {
        if (_impl.GetRawBucketsPointer() == other._impl.GetRawBucketsPointer())
            return true; // Reference equality short circuit
        
        if (_impl.IsCreated != other._impl.IsCreated)
            return false;
        
        if (Count != other.Count)
            return false;

        foreach (var item in other)
        {
            if (!Contains(item.Key, item.Value, comparer))
                return false;
        }
        
        return true;
    }

    // -- write access

    public ref TValue GetItemRef(in TKey key)
    {
        var hash = default(THash).ComputeHash(in key);
        var entryPtr = _impl.Find(key, hash);
        if (entryPtr.IsNull)
        {
            throw new InvalidOperationException("Key not found");
        }
        
        return ref entryPtr.Get().Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(in TKey key, TValue value)
    {
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
    public bool Add(in KeyValuePair<TKey, TValue> item)
        => Add(item.Key, item.Value);

    public void Clear()
        => _impl.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(in TKey key)
        => _impl.Remove(key, default(THash).ComputeHash(in key));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Assign(NativeDictionary<TKey, TValue, THash> other)
    {
        Clear();
        foreach (var item in other)
        {
            Add(item.Key, item.Value);
        }
    }
    
    // -- accessor
    
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
}