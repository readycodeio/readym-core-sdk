using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Extensions;

internal static class NetDataWriterExtensions
{
    public static void PutCustomRelayEventHeader(this NetDataWriter writer, RelayMessageCode eventCode, PlayerId playerId, RelayMode relayMode)
    {
        writer.PutMessageCode(eventCode);
        writer.Put(playerId);
        var flags = (byte)relayMode;
        writer.Put(flags);
    }
    
    public static CustomRelayEventHeader GetCustomRelayEventHeader(this NetDataReader reader, RelayMessageCode eventCode)
    {
        var sender = reader.Get<PlayerId>();
        var flags = reader.GetByte();

        var relayMode = (RelayMode)flags;
        return new CustomRelayEventHeader(eventCode, sender, null, relayMode);
    }
}
