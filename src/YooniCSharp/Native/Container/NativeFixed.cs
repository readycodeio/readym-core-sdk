using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential)]
public struct NativeFixed<T, TStorage>() : IEnumerable<T>
    where T : unmanaged
    where TStorage : unmanaged, IStorage<T>
{
    public struct Enumerator(NativeFixed<T, TStorage> impl) : IEnumerator<T>
    {
        private NativeFixed<T, TStorage> _impl = impl;
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
    
    private TypedArray<T, TStorage> _arr = default;
    private int _count = 0;

    [Pure]
    public int Count
        => _count;

    [Pure]
    public int Capacity
        => _arr.Length;

    public ref T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
            {
                throw new InvalidOperationException($"Index {index} is out of bounds: {0}..{_count}");
            }
            return ref _arr[index];
        }
    }

    public int Add(T value)
    {
        if (_count >= Capacity)
            throw new InvalidOperationException("Fixed collection is full");
        
        var index = _count;
        _arr[index] = value;
        ++_count;

        return index;
    }

    public void Insert(int index, T value)
    {
        if (_count >= Capacity)
            throw new InvalidOperationException("Fixed collection is full");
        
        if (index < 0 || index > _count)
            throw new InvalidOperationException($"Index {index} is out of bounds: {0}..{_count}");
        
        for (var i = (-(index + 1)) + _count; i >= 0; --i)
        {
            _arr[index + 1 + i] = _arr[index + i];
        }
        _arr[index] = value;
        _count++;
    }

    public void InsertRange(int index, T value, int count)
    {
        if (_count >= Capacity)
            throw new InvalidOperationException("Fixed collection is full");

        if (index < 0 || index > _count)
            throw new InvalidOperationException($"Index {index} is out of bounds: {0}..{_count}");
        
        for (var i = _count - 1; i >= index; i--)
        {
            _arr[i + count] = _arr[i];
        }
        for (var i = index; i < index + count; i++)
        {
            _arr[i] = value;
        }

        _count += count;
    }

    public void InsertRange(int index, NativeFixed<T, TStorage> source)
    {
        var sourceCount = source._count;

        if (_count + sourceCount > Capacity)
            throw new InvalidOperationException("Fixed collection is full");

        if (index < 0 || index > _count)
            throw new InvalidOperationException($"Index {index} is out of bounds: {0}..{_count}");
        
        for (var i = _count - 1; i >= index; i--)
        {
            _arr[i + sourceCount] = _arr[i];
        }
        for (var i = 0; i < sourceCount; i++)
        {
            _arr[index + i] = source._arr[i];
        }

        _count += sourceCount;
    }

    public T RemoveAt(int index)
    {
        if (index < 0 || index >= _count)
            throw new InvalidOperationException($"Index {index} is out of bounds: {0}..{_count}");
        
        var result = _arr[index];
        for (var i = 0; i < -1 + _count + (-index); ++i)
        {
            _arr[index + i] = _arr[index + 1 + i];
        }
        _count--;
        return result;
    }

    public T RemoveSwapBack(int index)
    {
        if (index < 0 || index >= _count)
            throw new InvalidOperationException($"Index {index} is out of bounds: {0}..{_count}");

        var result = _arr[index];
        _arr[index] = _arr[_count - 1];
        _count--;
        return result;
    }

    public void RemoveRange(int index, int count)
    {
        if (index < 0 || index + count > _count)
            throw new InvalidOperationException($"Index {index} is out of bounds: {0}..{_count}");

        for (var i = index; i < _count - count; ++i)
        {
            _arr[i] = _arr[i + count];
        }
        _count -= count;
    }

    public void Clear()
    {
        _count = 0;
    }

    public bool EnsureLength(int targetLength)
    {
        if (targetLength < 0 || targetLength > Capacity)
            throw new InvalidOperationException("Fixed collection is full");

        if (_count < targetLength)
        {
            for (var i = 0; i < targetLength - _count; ++i)
            {
                _arr[_count + i] = default;
            }
            _count = targetLength;
            return true;
        }
        return false;
    }

    public void Resize(int newLength)
    {
        if (newLength < 0 || newLength > Capacity)
            throw new InvalidOperationException("Fixed collection is full");

        if (_count < newLength)
        {
            for (var i = 0; i < newLength - _count; ++i)
            {
                _arr[_count + i] = default;
            }
        }
        _count = newLength;
    }

    public void ZeroMemory(int index, int count)
    {
        if (index < 0 || index + count > _count)
        {
            throw new InvalidOperationException($"Range starting at {index} is out of bounds: {0}..{_count}");
        }
        for (var i = index; i < count + index; ++i)
        {
            _arr[i] = default;
        }
    }

    public Enumerator GetEnumerator()
        => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}