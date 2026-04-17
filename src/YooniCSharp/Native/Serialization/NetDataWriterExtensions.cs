using System;
using LiteNetLib.Utils;

namespace Yooni.Native.Serialization;

public static class NetDataWriterExtensions
{
    public static unsafe void Put(this NetDataWriter writer, byte* value, int length)
    {
        writer.ResizeIfNeed(length);
        fixed (byte* ptr = writer.Data)
        {
            Buffer.MemoryCopy(value, ptr + writer.Length, length, length);
        }
        writer.SetPosition(writer.Length + length);
    }
}