using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential)]
public struct NativeRingBuffer<T, TStorage>() : IEnumerable<T>
    where T : unmanaged
    where TStorage : unmanaged, IStorage<T>
{
    public struct Enumerator(NativeRingBuffer<T, TStorage> impl) : IEnumerator<T>
    {
        private NativeRingBuffer<T, TStorage> _impl = impl;
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
    private int _head = 0;
    private int _count = 0;

    public int Count
        => _count;

    public int Capacity
        => _arr.Length;

    public void Clear()
    {
        _head = 0;
        _count = 0;
    }

    public bool Push(in T value)
    {
        if (_count < _arr.Length)
        {
            var writeIndex = (_head + _count) % _arr.Length;
            _arr[writeIndex] = value;
            ++_count;
        }
        else
        {
            _arr[_head] = value;
            _head = (_head + 1) % _arr.Length;
        }
        return true;
    }

    public void Pop()
    {
        if (_count > 0)
        {
            _head = (_head + 1) % _arr.Length;
            --_count;
        }
    }

    public ref T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new InvalidOperationException($"Index {index} is out of bounds: {0}..{_count}");
            return ref _arr[(_head + index) % _arr.Length];
        }
    }

    public ref T Newest
        => ref _arr[(_head + _count - 1) % _arr.Length];

    public ref T Oldest
        => ref _arr[_head];

    public Enumerator GetEnumerator()
        => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}