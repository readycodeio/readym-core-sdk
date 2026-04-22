using System;
using System.Diagnostics;
using LiteNetLib.Utils;
using Yooni.Native.Container;

namespace Yooni.Native.Serialization;

public static class NetSerializationExtensions
{
    public static unsafe void Serialize(this in NativeString64 str, NetDataWriter writer)
    {
        writer.EnsureFit(4 + 1 + str.Length);
        
        checked
        {
            writer.Put(str.Length);
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
        var length = reader.GetInt();
        var isWide = reader.GetBool();
        
        if (length < 0)
            throw new InvalidOperationException($"Invalid length for NativeString64: {length}");
        if (length > NativeString64.Capacity)
            throw new InvalidOperationException($"Length for NativeString64 exceeds capacity: length={length}, capacity={NativeString64.Capacity}");
        if (reader.AvailableBytes < length)
            throw new InvalidOperationException($"Not enough bytes to read NativeString64: required={length}, available={reader.AvailableBytes}");
        
        str = new NativeString64(reader.RawData, reader.Position, length, isWide);
        reader.SetPosition(reader.Position + length);
        
        Debug.Assert(reader.Position <= reader.RawData.Length, "Reader position exceeded raw data length after deserializing NativeString256");
    }
    
    public static unsafe void Serialize(this in NativeString256 str, NetDataWriter writer)
    {
        writer.EnsureFit(4 + 1 + str.Length);
        
        checked
        {
            writer.Put(str.Length);
        }
        writer.Put(str.IsWide);
        fixed (byte* ptr = writer.Data)
        {
            str.CopyTo(ptr + writer.Length);
        }
        writer.SetPosition(writer.Length + str.Length);
        
        Debug.Assert(writer.Length <= writer.Data.Length, "Writer position exceeded data length after serializing NativeString256");
    }
    
    public static void Deserialize(this ref NativeString256 str, NetDataReader reader)
    {
        var length = reader.GetInt();
        var isWide = reader.GetBool();
        
        if (length < 0)
            throw new InvalidOperationException($"Invalid length for NativeString256: {length}");
        if (length > NativeString256.Capacity)
            throw new InvalidOperationException($"Length for NativeString256 exceeds capacity: length={length}, capacity={NativeString256.Capacity}");
        if (reader.AvailableBytes < length)
            throw new InvalidOperationException($"Not enough bytes to read NativeString256: required={length}, available={reader.AvailableBytes}");
        
        str = new NativeString256(reader.RawData, reader.Position, length, isWide);
        reader.SetPosition(reader.Position + length);
        
        Debug.Assert(reader.Position <= reader.RawData.Length, "Reader position exceeded raw data length after deserializing NativeString256");
    }
}