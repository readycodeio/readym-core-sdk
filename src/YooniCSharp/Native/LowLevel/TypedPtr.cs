using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct TypedPtr<T> : IEquatable<TypedPtr<T>>
    where T : unmanaged
{
    private readonly void* _ptr;

    public TypedPtr(void* ptr)
    {
        _ptr = ptr;
    }

    public TypedPtr(ref T value)
    {
        _ptr = Unsafe.AsPointer(ref value);
    }

    public ref T Get()
        => ref Unsafe.AsRef<T>(_ptr);

    public bool Equals(TypedPtr<T> other)
        => _ptr == other._ptr;

    public override bool Equals(object? obj)
        => obj is TypedPtr<T> other && Equals(other);

    public override int GetHashCode()
        => unchecked((int)(long)_ptr);
    
    public bool IsNull
        => _ptr == null;

    public static TypedPtr<T> operator +(TypedPtr<T> ptr, int index)
        => new((byte*)ptr._ptr + index * sizeof(T));

    public static TypedPtr<T> operator -(TypedPtr<T> ptr, int index)
        => new((byte*)ptr._ptr - index * sizeof(T));

    public static int operator -(TypedPtr<T> ptr, TypedPtr<T> other)
        => (int)(((byte*)ptr._ptr - (byte*)other._ptr) / sizeof(T));
    
    public static TypedPtr<T> operator ++(TypedPtr<T> c)
        => new((byte*)c._ptr + sizeof(T));

    public static TypedPtr<T> operator --(TypedPtr<T> c)
        => new((byte*)c._ptr - sizeof(T));

    public static bool operator ==(TypedPtr<T> x, TypedPtr<T> y)
        => x._ptr == y._ptr;

    public static bool operator !=(TypedPtr<T> x, TypedPtr<T> y)
        => x._ptr != y._ptr;
    
    public static bool operator <(TypedPtr<T> x, TypedPtr<T> y)
        => x._ptr < y._ptr;
    
    public static bool operator >(TypedPtr<T> x, TypedPtr<T> y)
        => x._ptr > y._ptr;
    
    public static bool operator <=(TypedPtr<T> x, TypedPtr<T> y)
        => x._ptr <= y._ptr;
    
    public static bool operator >=(TypedPtr<T> x, TypedPtr<T> y)
        => x._ptr >= y._ptr;
    
    public static TypedPtr<T> Null
        => default;
    
    public static TypedPtr<T> Alloc(AllocatorKind kind)
        => new(Allocator.Alloc(sizeof(T), kind));
    
    public void Free(AllocatorKind kind)
        => Allocator.Free(_ptr, kind);

    public void* GetPointer()
        => _ptr;
}