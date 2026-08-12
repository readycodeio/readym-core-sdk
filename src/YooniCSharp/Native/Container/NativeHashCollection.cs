using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct NativeHashCollection<TKey, TValue> : IDisposable, IEnumerable<NativeHashCollection<TKey, TValue>.Entry>
    where TKey : unmanaged
    where TValue : unmanaged
{
    private static readonly EqualityComparer<TKey> _keyComparer = EqualityComparer<TKey>.Default;

    internal enum EntryState : uint
    {
        None = 0,
        Free = 1,
        Used = 2,
    }

    public struct Entry
    {
        public TypedPtr<Entry> Next;
        public uint Hash;
        public EntryState State;
        public TKey Key;
        public TValue Value;
    }

    public struct Enumerator : IEnumerator<Entry>
    {
        private readonly NativeHashCollection<TKey, TValue> _owner;
        private int _index;
        private Entry _current;

        internal Enumerator(NativeHashCollection<TKey, TValue> owner)
        {
            _owner = owner;
            _index = -1;
            _current = default;
        }

        public Entry Current => _current;

        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            while (true)
            {
                _index++;
                if (_index >= _owner._usedCount)
                {
                    _current = default;
                    return false;
                }

                var entry = _owner._entries[_index];
                if (entry.State == EntryState.Used)
                {
                    _current = entry;
                    return true;
                }
            }
        }

        public void Reset()
        {
            _index = -1;
            _current = default;
        }

        public void Dispose() { }
    }

    private static readonly int[] _primes =
    [
        3,
        7,
        17,
        29,
        53,
        97,
        193,
        389,
        769,
        1543,
        3079,
        6151,
        12289,
        24593,
        49157,
        98317,
        196613,
        393241,
        786433,
        1572869,
        3145739,
        6291469,
        12582917,
        25165843,
        50331653,
        100663319,
        201326611,
        402653189,
        805306457,
        1610612741
    ];

    private int _count;
    private int _usedCount;
    private int _freeCount;
    private TypedArrayPtr<TypedPtr<Entry>> _buckets;
    private TypedPtr<Entry> _freeHead;
    private TypedArrayPtr<Entry> _entries;
    private AllocatorKind _allocator;

    public bool IsCreated
        => !_buckets.IsNull;

    public AllocatorKind Allocator
        => _allocator;

    public Enumerator GetEnumerator()
        => new(this);

    IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);

    public static int GetNextPrime(int value)
    {
        value--;
        for (int i = 0; i < _primes.Length; i++)
        {
            var prime = _primes[i];

            if (prime > value)
            {
                return prime;
            }
        }

        throw new InvalidOperationException($"HashCollection can't get larger than {_primes[_primes.Length - 1]}");
    }

    public NativeHashCollection(int initialCapacity, AllocatorKind kind)
    {
        _count = GetNextPrime(initialCapacity);

        _buckets = TypedArrayPtr<TypedPtr<Entry>>.Alloc(_count, kind);
        _entries = TypedArrayPtr<Entry>.Alloc(_count, kind);
        _buckets.ZeroMemory(_count);
        _entries.ZeroMemory(_count);

        _freeHead = TypedPtr<Entry>.Null;
        _freeCount = 0;
        _usedCount = 0;

        _allocator = kind;
    }

    public void Dispose()
    {
        _buckets.Free(_allocator);
        _entries.Free(_allocator);
    }

    public int Count => _usedCount - _freeCount;

    public int Capacity => _count;

    public readonly TypedPtr<Entry> Find(TKey key, uint valueHash)
    {
        if (_count == 0)
        {
            return default;
        }

        var bucketHeadPtr = _buckets[(int)(valueHash % _count)];

        while (!bucketHeadPtr.IsNull)
        {
            ref var bucketHead = ref bucketHeadPtr.Get();
            if (bucketHead.Hash == valueHash && _keyComparer.Equals(key, bucketHead.Key))
            {
                return bucketHeadPtr;
            }
            else
            {
                bucketHeadPtr = bucketHead.Next;
            }
        }

        return default;
    }

    public bool Remove(TKey key, uint valueHash)
    {
        if (_count == 0)
        {
            return false;
        }

        var bucketHash = (int)(valueHash % _count);
        var bucketHeadPtr = _buckets[bucketHash];
        var bucketPrevPtr = default(TypedPtr<Entry>);

        while (!bucketHeadPtr.IsNull)
        {
            ref var bucketHead = ref bucketHeadPtr.Get();
            if (bucketHead.Hash == valueHash && _keyComparer.Equals(key, bucketHead.Key))
            {
                if (bucketPrevPtr.IsNull)
                {
                    _buckets[bucketHash] = bucketHead.Next;
                }
                else
                {
                    bucketPrevPtr.Get().Next = bucketHead.Next;
                }

                Debug.Assert(bucketHead.State == EntryState.Used);
                bucketHead.Next = _freeHead;
                bucketHead.State = EntryState.Free;
                _freeHead = bucketHeadPtr;
                _freeCount++;
                return true;
            }
            else
            {
                bucketPrevPtr = bucketHeadPtr;
                bucketHeadPtr = bucketHead.Next;
            }
        }

        return false;
    }

    public TypedPtr<Entry> Insert(TKey key, uint valueHash, TValue value)
    {
        TypedPtr<Entry> entryPtr;

        if (!_freeHead.IsNull)
        {
            Debug.Assert(_freeCount > 0);

            entryPtr = _freeHead;

            ref var entry = ref entryPtr.Get();
            _freeHead = entry.Next;
            _freeCount--;

            Debug.Assert(entry.State == EntryState.Free);
        }
        else
        {
            if (_usedCount == _count)
            {
                Expand();
            }

            entryPtr = _entries.GetPointer(_usedCount);
            _usedCount++;

            ref var entry = ref entryPtr.Get();
            Debug.Assert(entry.State == EntryState.None);
        }

        {
            var bucketHash = (int)(valueHash % _count);
            ref var entry = ref entryPtr.Get();
            entry.Hash = valueHash;
            entry.Next = _buckets[bucketHash];
            entry.State = EntryState.Used;
            entry.Key = key;
            entry.Value = value;

            _buckets[bucketHash] = entryPtr;
            return entryPtr;
        }
    }

    public void Clear()
    {
        _freeHead = default;
        _freeCount = 0;
        _usedCount = 0;

        _buckets.ZeroMemory(_count);
        _entries.ZeroMemory(_count);
    }

    private void Expand()
    {
        var capacity = GetNextPrime(_count + 1);

        var newBuckets = TypedArrayPtr<TypedPtr<Entry>>.Alloc(capacity, _allocator);
        newBuckets.ZeroMemory(capacity);

        var newEntries = TypedArrayPtr<Entry>.Alloc(capacity, _allocator);
        newEntries.ZeroMemory(capacity);
        newEntries.CopyMemory(_entries, _count);

        _freeHead = default;
        _freeCount = 0;

        for (var i = _count - 1; i >= 0; --i)
        {
            ref var entry = ref newEntries[i];
            if (entry.State == EntryState.Used)
            {
                var bucketHash = (int)(entry.Hash % capacity);
                entry.Next = newBuckets[bucketHash];
                newBuckets[bucketHash] = new TypedPtr<Entry>(ref entry);
            }
            else if (entry.State == EntryState.Free)
            {
                entry.Next = _freeHead;
                _freeHead = new TypedPtr<Entry>(ref entry);
                _freeCount++;
            }
        }

        _buckets.Free(_allocator);
        _entries.Free(_allocator);

        _buckets = newBuckets;
        _entries = newEntries;
        _count = capacity;
    }

    [Pure]
    internal IntPtr GetRawBucketsPointer()
        => _buckets.GetPointer(0).GetIntPtr();
}