using System;
using LiteNetLib.Utils;
using Yooni.Native.Container;

namespace Yooni.Native.Serialization;

/// How a native string's bytes are written after its length.
internal enum NativeStringWireForm : byte
{
    Narrow = 0, // 1 byte per character
    Wide = 1, // 2 bytes per character
    WideAsAscii = 2, // 1 byte per character, expanded after deserialization
}

public static class NetSerializationExtensions
{
    public static unsafe void Serialize(this in NativeString64 str, NetDataWriter writer)
    {
        var bytes = stackalloc byte[NativeString64.Capacity];
        str.CopyTo(bytes);
        Write(writer, bytes, str.Length, str.IsWide);
    }

    public static unsafe void Deserialize(this ref NativeString64 str, NetDataReader reader)
    {
        var bytes = stackalloc byte[NativeString64.Capacity];
        var length = Read(reader, bytes, NativeString64.Capacity, nameof(NativeString64), out var isWide);
        str = new NativeString64(bytes, length, isWide);
    }

    public static unsafe void Serialize(this in NativeString256 str, NetDataWriter writer)
    {
        var bytes = stackalloc byte[NativeString256.Capacity];
        str.CopyTo(bytes);
        Write(writer, bytes, str.Length, str.IsWide);
    }

    public static unsafe void Deserialize(this ref NativeString256 str, NetDataReader reader)
    {
        var bytes = stackalloc byte[NativeString256.Capacity];
        var length = Read(reader, bytes, NativeString256.Capacity, nameof(NativeString256), out var isWide);
        str = new NativeString256(bytes, length, isWide);
    }

    // [byte length][byte form][bytes]. The length counts the bytes on the wire, so an ASCII-only wide string of n
    // characters says n and is n * 2 bytes again once read
    private static unsafe void Write(NetDataWriter writer, byte* bytes, int length, bool isWide)
    {
        if (isWide && IsAscii(bytes, length))
        {
            writer.Put((byte)(length / 2));
            writer.Put((byte)NativeStringWireForm.WideAsAscii);

            for (var i = 0; i < length; i += 2)
                writer.Put(bytes[i]);

            return;
        }

        writer.Put((byte)length);
        writer.Put((byte)(isWide ? NativeStringWireForm.Wide : NativeStringWireForm.Narrow));

        writer.EnsureFit(length);
        fixed (byte* data = writer.Data)
        {
            Buffer.MemoryCopy(bytes, data + writer.Length, length, length);
        }

        writer.SetPosition(writer.Length + length);
    }

    // Every UTF-16 unit below 0x80: the low byte in range and the high byte zero
    private static unsafe bool IsAscii(byte* wide, int length)
    {
        for (var i = 0; i + 1 < length; i += 2)
        {
            if (wide[i] >= 0x80 || wide[i + 1] != 0)
                return false;
        }

        return true;
    }

    private static unsafe int Read(NetDataReader reader, byte* into, int capacity, string type, out bool isWide)
    {
        if (reader.AvailableBytes < 2)
            throw new InvalidOperationException($"The stream ends before a {type}'s length and form");

        var length = reader.GetByte();
        var form = (NativeStringWireForm)reader.GetByte();

        if (form > NativeStringWireForm.WideAsAscii)
            throw new InvalidOperationException($"Unknown {type} form {(byte)form}");

        var inMemory = form == NativeStringWireForm.WideAsAscii ? length * 2 : length;
        if (inMemory > capacity)
            throw new InvalidOperationException($"A {type} of {inMemory} bytes exceeds its capacity of {capacity}");
        if (reader.AvailableBytes < length)
            throw new InvalidOperationException($"The stream ends inside a {type}: it needs {length} bytes and has {reader.AvailableBytes}");

        var source = reader.RawData;
        var start = reader.Position;

        if (form == NativeStringWireForm.WideAsAscii)
        {
            for (var i = 0; i < length; i++)
            {
                into[i * 2] = source[start + i];
                into[i * 2 + 1] = 0;
            }
        }
        else
        {
            fixed (byte* from = source)
            {
                Buffer.MemoryCopy(from + start, into, capacity, length);
            }
        }

        reader.SetPosition(start + length);
        isWide = form != NativeStringWireForm.Narrow;
        return inMemory;
    }
}
