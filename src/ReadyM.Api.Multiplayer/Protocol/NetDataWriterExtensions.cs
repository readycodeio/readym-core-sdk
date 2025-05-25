using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Protocol;

public static class NetDataWriterExtensions
{
    public static void PutCustomEventHeader(this NetDataWriter writer, byte eventId, short playerId, RelayMode relayMode, EventCaching eventCaching)
    {
        writer.Put(eventId);
        writer.Put(playerId);
        // relayMode and eventCaching are both in [0..3], so we can pack both into a single byte
        var flags = (byte)((byte)relayMode | ((byte)eventCaching << 2));
        writer.Put(flags);
    }

    public static void PutCustomEventHeader(this NetDataWriter writer, byte eventId, short playerId, int[] peers, EventCaching eventCaching)
    {
        writer.Put(eventId);
        writer.Put(playerId);
        var flags = (byte)((byte)RelayMode.Peers | ((byte)eventCaching << 2));
        writer.Put(flags);
        writer.PutArray(peers);
    }

    public static CustomEventHeader GetCustomEventHeader(this NetDataReader reader, byte eventCode)
    {
        var sender = reader.GetShort();
        var flags = reader.GetByte();

        var relayMode = (RelayMode)(flags & 0b11);
        var eventCaching = (EventCaching)(flags >> 2);

        if (relayMode == RelayMode.Peers)
        {
            var peers = reader.GetIntArray();
            return new CustomEventHeader(eventCode, sender, peers, relayMode, eventCaching);
        }

        return new CustomEventHeader(eventCode, sender, null, relayMode, eventCaching);
    }
}