using System.IO;
using LiteNetLib.Utils;

namespace ReadyM.Api.Serialization;

public static class VarUShortExtensions
{
    public static void PutVarUShort(this NetDataWriter writer, ushort value)
    {
        var remaining = (uint)value;
        while (remaining >= 0x80)
        {
            writer.Put((byte)(remaining | 0x80));
            remaining >>= 7;
        }

        writer.Put((byte)remaining);
    }

    /// Throws <see cref="InvalidDataException"/> for more than three bytes, more than 16 bits, or a stream that ends first.
    public static ushort GetVarUShort(this NetDataReader reader)
    {
        var value = 0u;

        for (var shift = 0; shift < 14; shift += 7)
        {
            var b = NextByte(reader);
            value |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return (ushort)value;
        }

        // The third byte holds the top 2 bits and has to be the last
        var last = NextByte(reader);
        if ((last & 0x80) != 0)
            throw new InvalidDataException($"A varint takes more than {VarUShort.MaxSize} bytes");
        if (last > 0x03)
            throw new InvalidDataException($"A varint's third byte is 0x{last:X2}, which goes past 16 bits");

        return (ushort)(value | ((uint)last << 14));
    }

    private static byte NextByte(NetDataReader reader)
    {
        if (reader.AvailableBytes == 0)
            throw new InvalidDataException("The stream ends in the middle of a varint");

        return reader.GetByte();
    }
}
