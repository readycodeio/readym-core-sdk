using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace Yooni.Native.Container;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct NativeString256 : IEquatable<NativeString256>, INativeString
{
    private const int CharBufferLength = 255;
    private const int ByteBufferLength = Capacity * 4; // each UTF-8 char takes up to 4 bytes
    public const int Capacity = CharBufferLength - 1;

    private readonly byte _length;
    private fixed byte _bytes[CharBufferLength];

    [Pure]
    public string ToManaged()
    {
        fixed (byte* p = _bytes)
        {
            return new string((sbyte*) p, 0, _length, Encoding.UTF8);
        }
    }

    [Pure]
    public byte* GetChars()
    {
        fixed (byte* ptr = _bytes)
            return ptr;
    }

    [Pure]
    public int Length
        => _length;
    
    [Pure]
    int INativeString.Capacity
        => Capacity;

    public NativeString256(byte* bytes, int length)
    {
        if (length < 0)
            throw new InvalidOperationException();
        if (length > ByteBufferLength - 1) // leave space for null terminator
            throw new InvalidOperationException();

        _length = (byte) length;
        fixed (byte* p = _bytes)
        {
            for (var i = 0; i < length; i++)
            {
                *(p + i) = *(bytes + i);
            }

            *(p + length) = (byte) '\0';
        }
    }

    public NativeString256(string? value)
    {
        if (value is null)
        {
            _length = 0;
            return;
        }

        var charCount = Math.Min(value.Length, Capacity);
        var buffer = stackalloc byte[ByteBufferLength];

        fixed (char* charPtr = value)
        {
            _length = (byte) Encoding.UTF8.GetBytes(charPtr, charCount, buffer, ByteBufferLength);
        }

        if (_length > Capacity)
        {
            Debug.WriteLine($"String \"{value}\" is too long to be converted to an UnmanagedString256");
            _length = Capacity;
        }

        fixed (byte* p = _bytes)
        {
            for (var i = 0; i < _length; i++)
            {
                *(p + i) = buffer[i];
            }

            *(p + _length) = (byte) '\0';
        }
    }

    [Pure]
    public bool Equals(NativeString256 other)
        => this == other;

    [Pure]
    public override bool Equals(object? obj)
        => obj is NativeString256 other && Equals(other);

    [Pure]
    public bool StartsWith(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var valueBytes = stackalloc byte[ByteBufferLength];
        fixed (char* valuePtr = value)
        {
            var valueByteLength = Encoding.UTF8.GetBytes(valuePtr, value.Length, valueBytes, ByteBufferLength);
            if (valueByteLength > _length)
                return false;

            fixed (byte* p = _bytes)
            {
                for (var i = 0; i < valueByteLength; i++)
                {
                    if (*(p + i) != valueBytes[i])
                        return false;
                }
            }
        }

        return true;
    }

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

    public static bool operator ==(NativeString256 x, string? y)
    {
        if (x._bytes == null && y == null)
            return true;
        if (x._bytes == null || y == null)
            return false;

        var yBytes = stackalloc byte[ByteBufferLength];
        fixed (char* yPtr = y)
        {
            var yByteLength = Encoding.UTF8.GetBytes(yPtr, y.Length, yBytes, ByteBufferLength);
            if (x._length != yByteLength)
                return false;
        }

        for (var i = 0; i < x._length; i++)
        {
            if (*(x._bytes + i) != yBytes[i])
                return false;
        }

        return true;
    }

    public static bool operator !=(NativeString256 x, string? y)
        => !(x == y);

    public static bool operator ==(string x, NativeString256 y)
        => y == x;

    public static bool operator !=(string x, NativeString256 y)
        => y != x;

    public static bool operator ==(NativeString256 x, NativeString256 y)
    {
        if (x._bytes == y._bytes)
            return true;
        if (x._bytes == null || y._bytes == null)
            return false;

        if (x._length != y._length)
            return false;

        for (var i = 0; i < x._length; i++)
        {
            if (*(x._bytes + i) != *(y._bytes + i))
                return false;
        }

        return true;
    }

    public static bool operator !=(NativeString256 x, NativeString256 y) => !(x == y);

    public static NativeString256 Null => default;
}