using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Extensions;

/// <summary>Writes and reads a message's code, as a <see cref="VarUShort"/>: one byte below 128.</summary>
internal static class RelayMessageCodeSerializationExtensions
{
    public static void PutMessageCode(this NetDataWriter writer, RelayMessageCode code)
        => writer.PutVarUShort((ushort)code);

    public static RelayMessageCode GetMessageCode(this NetDataReader reader)
        => (RelayMessageCode)reader.GetVarUShort();
}
