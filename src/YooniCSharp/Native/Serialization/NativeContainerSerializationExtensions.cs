using System;
using LiteNetLib.Utils;
using Yooni.Native.Container;

namespace Yooni.Native.Serialization;

public static class NetSerializationExtensions
{
    public static unsafe void Serialize(this in NativeString64 str, NetDataWriter writer)
    {
        writer.ResizeIfNeed(writer.Length + str.Length + sizeof(byte) + sizeof(byte));
        checked
        {
            writer.Put((byte)str.Length);
        }
        writer.Put(str.IsWide);
        fixed (byte* ptr = writer.Data)
        {
            str.CopyTo(ptr + writer.Length);
        }
        writer.SetPosition(writer.Length + str.Length);
    }
    
    public static void Deserialize(this ref NativeString64 str, NetDataReader reader)
    {
        var length = reader.GetByte();
        var isWide = reader.GetBool();
        
        if (reader.AvailableBytes < length)
            throw new InvalidOperationException($"Not enough bytes to read NativeString64: required={length}, available={reader.AvailableBytes}");
        
        str = new NativeString64(reader.RawData, reader.Position, length, isWide);
        reader.SetPosition(reader.Position + length);
    }
    
    public static unsafe void Serialize(this in NativeString256 str, NetDataWriter writer)
    {
        writer.ResizeIfNeed(writer.Length + str.Length + sizeof(byte) + sizeof(byte));
        checked
        {
            writer.Put((byte)str.Length);
        }
        writer.Put(str.IsWide);
        fixed (byte* ptr = writer.Data)
        {
            str.CopyTo(ptr + writer.Length);
        }
        writer.SetPosition(writer.Length + str.Length);
    }
    
    public static void Deserialize(this ref NativeString256 str, NetDataReader reader)
    {
        var length = reader.GetByte();
        var isWide = reader.GetBool();
        
        if (reader.AvailableBytes < length)
            throw new InvalidOperationException($"Not enough bytes to read NativeString256: required={length}, available={reader.AvailableBytes}");
        
        str = new NativeString256(reader.RawData, reader.Position, length, isWide);
        reader.SetPosition(reader.Position + length);
    }
}