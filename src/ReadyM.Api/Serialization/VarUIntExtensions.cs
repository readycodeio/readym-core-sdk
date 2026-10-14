using System.IO;
using LiteNetLib.Utils;

namespace ReadyM.Api.Serialization;

public static class VarUIntExtensions
{
    public static void PutVarUInt(this NetDataWriter writer, uint value)
    {
        while (value >= 0x80)
        {
            writer.Put((byte)(value | 0x80));
            value >>= 7;
        }

        writer.Put((byte)value);
    }

    /// Throws <see cref="InvalidDataException"/> for more than five bytes, more than 32 bits, or a stream that ends first.
    public static uint GetVarUInt(this NetDataReader reader)
    {
        var value = 0u;

        for (var shift = 0; shift < 28; shift += 7)
        {
            var b = NextByte(reader);
            value |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return value;
        }

        // The fifth byte holds the top 4 bits and has to be the last
        var last = NextByte(reader);
        if ((last & 0x80) != 0)
            throw new InvalidDataException($"A varint takes more than {VarUInt.MaxSize} bytes");
        if (last > 0x0F)
            throw new InvalidDataException($"A varint's fifth byte is 0x{last:X2}, which goes past 32 bits");

        return value | ((uint)last << 28);
    }

    private static byte NextByte(NetDataReader reader)
    {
        if (reader.AvailableBytes == 0)
            throw new InvalidDataException("The stream ends in the middle of a varint");

        return reader.GetByte();
    }
}
