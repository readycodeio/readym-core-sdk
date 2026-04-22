using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct NativeString256 : IEquatable<NativeString256>, INativeString
{
    public const int Size = 256;
    public const int Capacity = Size - sizeof(byte) - sizeof(byte);
    private const int ByteBufferLength = (Capacity + 1) * 4; // each UTF-8 char takes up to 4 bytes

    private readonly byte _length;
    private readonly byte _isWide;
    private fixed byte _bytes[Capacity];

    [Pure]
    public bool IsWide
        => _isWide != 0;
    
    [Pure]
    public string ToManaged()
    {
        fixed (byte* p = _bytes)
        {
            if (_isWide != 0)
                return new string((sbyte*) p, 0, _length, Encoding.Unicode);
            else
                return new string((sbyte*) p, 0, _length, Encoding.UTF8);
        }
    }

    [Pure]
    public void CopyTo(byte* dest)
    {
        fixed (byte* p = _bytes)
        {
            Buffer.MemoryCopy(p, dest, _length, _length);
        }
    }
    
    [Pure]
    int INativeString.Capacity
        => Capacity;

    [Pure]
    public int Length
        => _length;
    
    public byte this[int index]
    {
        get
        {
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException();
            return _bytes[index];
        }
    }

    public NativeString256(byte* bytes, int length, bool isWide)
    {
        if (length < 0)
            throw new InvalidOperationException();
        if (length > Capacity)
            throw new InvalidOperationException();

        _length = (byte) length;
        _isWide = (byte)(isWide ? 1 : 0);
        fixed (byte* p = _bytes)
        {
            for (var i = 0; i < length; i++)
            {
                *(p + i) = *(bytes + i);
            }
        }
    }
    
    public NativeString256(byte[] bytes, int length, bool isWide)
    {
        if (length < 0)
            throw new InvalidOperationException();
        if (length > Capacity)
            throw new InvalidOperationException();

        _length = (byte) length;
        _isWide = (byte)(isWide ? 1 : 0);
        fixed (byte* p = _bytes)
        {
            for (var i = 0; i < length; i++)
            {
                *(p + i) = bytes[i];
            }
        }
    }
    
    public NativeString256(byte[] bytes, int offset, int length, bool isWide)
    {
        if (length < 0)
            throw new InvalidOperationException();
        if (length > Capacity)
            throw new InvalidOperationException();

        _length = (byte) length;
        _isWide = (byte)(isWide ? 1 : 0);
        fixed (byte* p = _bytes)
        {
            for (var i = 0; i < length; i++)
            {
                *(p + i) = bytes[offset + i];
            }
        }
    }

    public NativeString256(string? value, bool useWide)
    {
        if (value is null)
        {
            _length = 0;
            return;
        }

        var buffer = stackalloc byte[ByteBufferLength];

        int result;
        fixed (char* charPtr = value)
        {
            if (useWide)
                result = Encoding.Unicode.GetBytes(charPtr, value.Length, buffer, ByteBufferLength);
            else
                result = Encoding.UTF8.GetBytes(charPtr, value.Length, buffer, ByteBufferLength);
        }

        if (result > Capacity)
            throw new InvalidOperationException($"String \"{value}\" is too long to be converted to an UnmanagedString256");

        _length = (byte)result;
        _isWide = (byte)(useWide ? 1 : 0);
        fixed (byte* p = _bytes)
        {
            for (var i = 0; i < _length; i++)
            {
                *(p + i) = buffer[i];
            }
        }
    }

    [Pure]
    public bool Equals(in NativeString256 other)
        => this == other;

    [Pure]
    public bool Equals(NativeString256 other)
        => this == other;

    [Pure]
    public override bool Equals(object? obj)
        => obj is NativeString256 other && Equals(other);

    [Pure]
    public override int GetHashCode()
    {
        var result = (int) _length;

        fixed (byte* p = _bytes)
        {
            for (var i = 0; i < _length; i++)
            {
                result *= 397;
                result ^= *(p + i);
            }
        }

        return result;
    }

    [Pure]
    public override string ToString()
        => ToManaged();

    public bool Equals(string? str)
        => this == str;

    public static bool operator ==(in NativeString256 x, string? y)
    {
        fixed (byte* xPtr = x._bytes)
        {
            if (xPtr == null && y == null)
                return true;
            if (xPtr == null || y == null)
                return false;

            var yBytes = stackalloc byte[ByteBufferLength];
            fixed (char* yPtr = y)
            {
                int yByteLength;
                if (x._isWide != 0)
                    yByteLength = Encoding.Unicode.GetBytes(yPtr, y.Length, yBytes, ByteBufferLength);
                else
                    yByteLength = Encoding.UTF8.GetBytes(yPtr, y.Length, yBytes, ByteBufferLength);
                if (x._length != yByteLength)
                    return false;
            }

            for (var i = 0; i < x._length; i++)
            {
                if (*(xPtr + i) != yBytes[i])
                    return false;
            }

            return true;
        }
    }

    public static bool operator !=(in NativeString256 x, string? y)
        => !(x == y);

    public static bool operator ==(string x, in NativeString256 y)
        => y == x;

    public static bool operator !=(string x, in NativeString256 y)
        => y != x;

    public static bool operator ==(in NativeString256 x, in NativeString256 y)
    {
        fixed (byte* xPtr = x._bytes, yPtr = y._bytes)
        {
            if (xPtr == yPtr)
                return true;
            if (xPtr == null || yPtr == null)
                return false;

            if (x._length != y._length)
                return false;
            if (x._isWide != y._isWide)
                return false;

            for (var i = 0; i < x._length; i++)
            {
                if (*(xPtr + i) != *(yPtr + i))
                    return false;
            }

            return true;
        }
    }

    public static bool operator !=(NativeString256 x, NativeString256 y) => !(x == y);

    public static NativeString256 Null => default;
}