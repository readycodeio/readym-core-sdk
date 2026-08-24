using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yooni.Native.Logging;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential)]
public struct NativeList<T> : IEnumerable<T>, IDisposable
    where T : unmanaged
{
    public struct Enumerator(NativeList<T> impl) : IEnumerator<T>
    {
        private NativeList<T> _impl = impl;
        private int _index = -1;

        public void Dispose()
        {
            _index = -1;
            _impl = default;
        }

        public T Current => _impl[_index];
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < _impl.Count;
        }

        public void Reset()
        {
            _index = -1;
        }
    }

    public readonly ref struct ReadOnly(NativeList<T> owner) : IEnumerable<T>
    {
        internal readonly NativeList<T> _impl = owner;

        public AllocatorKind Allocator
            => _impl.Allocator;

        public bool IsCreated
            => _impl.IsCreated;

        public int Count
            => _impl.Count;

        public int Capacity
            => _impl.Capacity;

        public ref readonly T this[int index]
            => ref _impl[index];

        // -- read access

        public bool Contains(in T value)
            => _impl.Contains(value);

        public bool Contains<TComparer>(in T value, TComparer comparer)
            where TComparer : IEqualityComparer<T>
            => _impl.Contains(value, comparer);

        public bool Equals(in NativeList<T> other)
            => _impl.Equals(other);

        public bool Equals(in ReadOnly other)
            => _impl.Equals(other._impl);

        public bool Equals<TComparer>(in NativeList<T> other, TComparer comparer)
            where TComparer : IEqualityComparer<T>
            => _impl.Equals(other, comparer);

        public bool Equals<TComparer>(in ReadOnly other, TComparer comparer)
            where TComparer : IEqualityComparer<T>
            => _impl.Equals(other._impl, comparer);

        // -- access object

        public Enumerator GetEnumerator()
            => new Enumerator(_impl);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new Enumerator(_impl);

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    private TypedArrayPtr<T> _ptr;
    private int _count;
    private int _capacity;
    private TrackedAllocator _allocator;

    private readonly void EnsureCreated()
    {
        if (!IsCreated)
            throw new InvalidOperationException("NativeList is not created");
    }

    public AllocatorKind Allocator
        => _allocator.Kind;

    [Pure]
    public readonly bool IsCreated
        => !_ptr.IsNull;

    [Pure]
    public readonly int Count
    {
        get
        {
            EnsureCreated();
            _allocator.Check();
            return _count;
        }
    }

    [Pure]
    public readonly int Capacity
    {
        get
        {
            EnsureCreated();
            _allocator.Check();
            return _capacity;
        }
    }

    // ReSharper disable once ConvertToPrimaryConstructor
    public NativeList(int initialCapacity, AllocatorKind kind, NativeLogLevel logLevel = NativeLogLevel.Disabled)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

        _ptr = TypedArrayPtr<T>.Alloc(initialCapacity, kind);
        _count = 0;
        _capacity = initialCapacity;
        _allocator = new TrackedAllocator(kind, logLevel);
    }

    public void Dispose()
    {
        EnsureCreated();
        var kind = _allocator.Kind;
        _allocator.Free();
        _capacity = 0;
        _count = 0;
        _ptr.Free(kind);
    }

    public void TryCreate(AllocatorKind kind, NativeLogLevel trackLevel = NativeLogLevel.Disabled)
    {
        if (IsCreated)
        {
            _allocator.Check();
            return;
        }

        _ptr = TypedArrayPtr<T>.Alloc(0, kind);
        _count = 0;
        _capacity = 0;
        _allocator = new TrackedAllocator(kind, trackLevel);
    }

    public ref T this[int index]
    {
        get
        {
            EnsureCreated();
            _allocator.Check();
            if (index < 0 || index >= _count)
            {
                throw new IndexOutOfRangeException($"Index {index} out of bounds 0..{_count}");
            }
            return ref _ptr[index];
        }
    }

    // -- read access

    public readonly bool Contains(in T value)
    {
        EnsureCreated();
        _allocator.Check();
        for (var i = 0; i < _count; ++i)
        {
            if (EqualityComparer<T>.Default.Equals(_ptr[i], value))
                return true;
        }
        return false;
    }

    public readonly bool Contains<TComparer>(in T value, TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        EnsureCreated();
        _allocator.Check();
        for (var i = 0; i < _count; ++i)
        {
            if (comparer.Equals(_ptr[i], value))
                return true;
        }
        return false;
    }

    public readonly bool Equals(in NativeList<T> other)
    {
        if (IsCreated) _allocator.Check();
        if (other.IsCreated) other._allocator.Check();

        if (_ptr == other._ptr)
            return true; // Reference equality short circuit

        if (IsCreated != other.IsCreated)
            return false;

        if (!IsCreated)
            return true;

        if (_count != other._count)
            return false;

        for (var i = 0; i < _count; ++i)
        {
            if (!EqualityComparer<T>.Default.Equals(_ptr[i], other._ptr[i]))
                return false;
        }
        return true;
    }

    public readonly bool Equals<TComparer>(in NativeList<T> other, TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        if (IsCreated) _allocator.Check();
        if (other.IsCreated) other._allocator.Check();

        if (_ptr == other._ptr)
            return true; // Reference equality short circuit

        if (IsCreated != other.IsCreated)
            return false;

        if (!IsCreated)
            return true;

        if (_count != other._count)
            return false;

        for (var i = 0; i < _count; ++i)
        {
            if (!comparer.Equals(_ptr[i], other._ptr[i]))
                return false;
        }
        return true;
    }

    // -- write access

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Assign(NativeList<T> other)
    {
        EnsureCreated();
        _allocator.Check();
        other.EnsureCreated();
        other._allocator.Check();
        if (_ptr == other._ptr)
            return;
        _allocator.MarkChangeNoCheck();
        Clear();
        foreach (var item in other)
        {
            Add(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Assign(ReadOnly other)
        => Assign(other._impl);

    public bool TrySet(int index, T value)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} out of bounds 0..{_count}");

        if (EqualityComparer<T>.Default.Equals(_ptr[index], value))
            return false;

        _ptr[index] = value;
        return true;
    }

    public int Add(T value)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (_capacity < _count + 1)
        {
            var newCapacity = (_count + 1) * 2;
            Realloc(newCapacity);
        }

        var index = _count;
        _ptr[index] = value;
        ++_count;

        return index;
    }

    public void Insert(int index, T value)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (index < 0 || index > _count)
            throw new IndexOutOfRangeException($"Index {index} out of bounds 0..{_count}");

        if (_capacity < _count + 1)
        {
            var newCapacity = (_count + 1) * 2;
            Realloc(newCapacity);
        }
        for (var i = (-(index + 1)) + _count; i >= 0; --i)
        {
            _ptr[index + 1 + i] = _ptr[index + i];
        }
        _ptr[index] = value;
        _count++;
    }

    public void InsertRange(int index, T value, int count)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        if (index < 0 || index > _count)
            throw new IndexOutOfRangeException($"Index {index} out of bounds 0..{_count}");

        if (count > int.MaxValue - _count)
            throw new ArgumentOutOfRangeException(nameof(count), "Resulting list length is too large");

        if (_capacity < _count + count)
        {
            var newCapacity = (_count + count) * 2;
            Realloc(newCapacity);
        }

        for (var i = _count - 1; i >= index; i--)
        {
            _ptr[i + count] = _ptr[i];
        }
        for (var i = index; i < index + count; i++)
        {
            _ptr[i] = value;
        }

        _count += count;
    }

    public void InsertRange(int index, NativeList<T> source)
    {
        EnsureCreated();
        _allocator.MarkChange();
        source.EnsureCreated();
        source._allocator.Check();

        if (index < 0 || index > _count)
            throw new IndexOutOfRangeException($"Index {index} out of bounds 0..{_count}");

        var sourceCount = source._count;

        if (sourceCount > int.MaxValue - _count)
            throw new ArgumentOutOfRangeException(nameof(source), "Resulting list length is too large");

        if (_capacity < _count + sourceCount)
        {
            var newCapacity = (_count + sourceCount) * 2;
            Realloc(newCapacity);
        }

        for (var i = _count - 1; i >= index; i--)
        {
            _ptr[i + sourceCount] = _ptr[i];
        }
        for (var i = 0; i < sourceCount; i++)
        {
            _ptr[index + i] = source._ptr[i];
        }

        _count += sourceCount;
    }

    public T RemoveAt(int index)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} out of bounds 0..{_count}");

        var result = _ptr[index];
        for (var i = 0; i < -1 + _count + (-index); ++i)
        {
            _ptr[index + i] = _ptr[index + 1 + i];
        }
        _count--;
        return result;
    }

    public T RemoveSwapBack(int index)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"Index {index} out of bounds 0..{_count}");

        var result = _ptr[index];

        _ptr[index] = _ptr[_count - 1];
        _count--;
        return result;
    }

    public void RemoveRange(int index, int count)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        if (count > _count || index < 0 || index > _count - count)
            throw new IndexOutOfRangeException($"Range {index}..{index + count} out of bounds 0..{_count}");

        for (var i = index; i < _count - count; ++i)
        {
            _ptr[i] = _ptr[i + count];
        }
        _count -= count;
    }

    public void Clear()
    {
        EnsureCreated();
        _allocator.MarkChange();
        _count = 0;
    }

    public bool EnsureLength(int targetLength)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (targetLength < 0)
            throw new ArgumentOutOfRangeException(nameof(targetLength), "Target length must be non-negative");

        if (_capacity < targetLength)
        {
            var newCapacity = targetLength * 2;
            Realloc(newCapacity);
        }
        if (_count < targetLength)
        {
            for (var i = 0; i < targetLength - _count; ++i)
            {
                _ptr[_count + i] = default;
            }
            _count = targetLength;
            return true;
        }
        return false;
    }

    public void Resize(int newLength)
    {
        EnsureCreated();
        _allocator.MarkChange();

        if (newLength < 0)
            throw new ArgumentOutOfRangeException(nameof(newLength), "New length must be non-negative");

        if (_capacity < newLength)
        {
            var newCapacity = newLength * 2;
            Realloc(newCapacity);
        }
        if (_count < newLength)
        {
            for (var i = 0; i < newLength - _count; ++i)
            {
                _ptr[_count + i] = default;
            }
        }
        _count = newLength;
    }

    public void ZeroMemory(int index, int count)
    {
        EnsureCreated();
        _allocator.Check();

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        if (count > _count || index < 0 || index > _count - count)
            throw new IndexOutOfRangeException($"Range {index}..{index + count} out of bounds 0..{_count}");

        for (var i = index; i < count + index; ++i)
        {
            _ptr[i] = default;
        }
    }

    private void Realloc(int newCapacity)
    {
        if (newCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(newCapacity), "New capacity must be non-negative");

        var prevPtr = _ptr;
        if (newCapacity > 0)
        {
            _ptr = TypedArrayPtr<T>.Alloc(newCapacity, _allocator.Kind);
            for (var i = 0; i < _count; ++i)
            {
                _ptr[i] = prevPtr[i];
            }
        }
        if (_capacity > 0)
        {
            prevPtr.Free(_allocator.Kind);
        }
        _capacity = newCapacity;
    }

    // -- access object

    public readonly Enumerator GetEnumerator()
    {
        EnsureCreated();
        _allocator.Check();
        return new Enumerator(this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        EnsureCreated();
        _allocator.Check();
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public readonly ReadOnly AsReadOnly()
    {
        EnsureCreated();
        _allocator.Check();
        return new ReadOnly(this);
    }

    public void Check()
        => _allocator.Check();

    internal void MarkChange()
        => _allocator.MarkChange();

    public void LogTracking(NativeLogLevel level = NativeLogLevel.Enabled)
        => _allocator.SetLogging(level);
}
