using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using Yooni.Native.Container;
using Yooni.Native.Serialization;

namespace ReadyM.Api.Multiplayer.Interning;

/// Writes and reads a string marked [Interned]. Called by generated code.
public static class InternedStringCodec
{
    // One varint: 0 when the string follows, and the id plus one otherwise
    private const uint InlineString = 0;

    public static void Write(NetDataWriter writer, in NativeString256 value)
    {
        if (InternedStrings.Current is { } index && index.TryGetIdForWrite(value, out var id))
        {
            writer.PutVarUInt(id + 1);
            return;
        }

        writer.PutVarUInt(InlineString);
        value.Serialize(writer);
    }

    public static void Read(NetDataReader reader, ref NativeString256 value)
    {
        var encoded = reader.GetVarUInt();
        if (encoded == InlineString)
        {
            value.Deserialize(reader);
            return;
        }

        var id = encoded - 1;

        // An id the reader cannot resolve has already been read past, so the rest of the message reads correctly
        if (InternedStrings.Current is not { } index)
        {
            // Nothing to report through; registration keeps marked fields out of the plugin host, the one reader without an index
            value = default;
        }
        else if (index.TryResolve(id, out var name))
        {
            value = name;
        }
        else
        {
            value = default;
            index.ReportUnresolved(id);
        }
    }
}
