using System;
using LiteNetLib.Utils;

namespace Yooni.Native.Serialization;

public static class NetDataReaderExtensions
{
    public static unsafe void GetRawBytes(this NetDataReader reader, byte* buffer, int length)
    {
        if (reader.AvailableBytes < length)
            throw new InvalidOperationException($"Not enough data to read. Required: {length}, available: {reader.AvailableBytes}");
        
        fixed (byte* ptr = reader.RawData)
        {
            Buffer.MemoryCopy(ptr + reader.Position, buffer, length, length);
        }
        reader.SkipBytes(length);
    }
}