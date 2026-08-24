using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct TypedArrayPtr<T> : IEquatable<TypedArrayPtr<T>>
    where T : unmanaged
{
    private void* _ptr;

    public TypedArrayPtr(void* ptr)
    {
        _ptr = ptr;
    }

    public TypedArrayPtr(ref T value)
    {
        _ptr = Unsafe.AsPointer(ref value);
    }

    public ref T this[int index]
        => ref Unsafe.AsRef<T>(((byte*)_ptr) + index * sizeof(T));

    public bool Equals(TypedArrayPtr<T> other)
        => _ptr == other._ptr;

    public override bool Equals(object? obj)
        => obj is TypedArrayPtr<T> other && Equals(other);

    public override int GetHashCode()
        => unchecked((int)(long)_ptr);

    public bool IsNull
        => _ptr == null;

    public static bool operator ==(TypedArrayPtr<T> x, TypedArrayPtr<T> y)
        => x._ptr == y._ptr;

    public static bool operator !=(TypedArrayPtr<T> x, TypedArrayPtr<T> y)
        => x._ptr != y._ptr;

    public static bool operator <(TypedArrayPtr<T> x, TypedArrayPtr<T> y)
        => x._ptr < y._ptr;

    public static bool operator >(TypedArrayPtr<T> x, TypedArrayPtr<T> y)
        => x._ptr > y._ptr;

    public static bool operator <=(TypedArrayPtr<T> x, TypedArrayPtr<T> y)
        => x._ptr <= y._ptr;

    public static bool operator >=(TypedArrayPtr<T> x, TypedArrayPtr<T> y)
        => x._ptr >= y._ptr;

    public static TypedArrayPtr<T> Null
        => default;

    public static TypedArrayPtr<T> Alloc(int count, AllocatorKind kind)
        => new(Allocator.Alloc(sizeof(T) * count, kind));

    public void Free(AllocatorKind kind)
        => Allocator.Free(ref _ptr, kind);

    public void ZeroMemory(int count)
        => MemoryUtils.ZeroMemory((byte*)_ptr, sizeof(T) * count);

    public void CopyMemory(TypedArrayPtr<T> fromEntries, int count)
        => MemoryUtils.CopyMemory((byte*)_ptr, (byte*)fromEntries._ptr, sizeof(T) * count);

    public TypedPtr<T> GetPointer(int index)
        => new(((byte*)_ptr) + index * sizeof(T));
}
