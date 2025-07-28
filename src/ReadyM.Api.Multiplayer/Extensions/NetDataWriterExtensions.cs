using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Extensions;

public static class NetDataWriterExtensions
{
    public static void PutCustomEventHeader(this NetDataWriter writer, RelayMessageCode eventCode, PlayerId playerId, RelayMode relayMode)
    {
        if (relayMode == RelayMode.Peers)
            throw new ArgumentException("Use PutCustomEventHeader with PlayerId[] for RelayMode.Peers", nameof(relayMode));
        writer.Put((byte)eventCode);
        writer.Put(playerId);
        var flags = (byte)relayMode;
        writer.Put(flags);
    }

    public static void PutCustomEventHeader(this NetDataWriter writer, RelayMessageCode eventCode, PlayerId playerId, PlayerId[] peers)
    {
        writer.Put((byte)eventCode);
        writer.Put(playerId);
        var flags = (byte)RelayMode.Peers;
        writer.Put(flags);
        writer.Put((ushort)peers.Length);
        foreach (var peerId in peers)
        {
            writer.Put(peerId);
        }
    }

    public static CustomEventHeader GetCustomEventHeader(this NetDataReader reader, RelayMessageCode eventCode)
    {
        var sender = reader.Get<PlayerId>();
        var flags = reader.GetByte();

        var relayMode = (RelayMode)flags;

        if (relayMode == RelayMode.Peers)
        {
            var peersLength = reader.GetUShort();
            var peers = new PlayerId[peersLength];
            for (var i = 0; i < peersLength; i++)
            {
                peers[i] = reader.Get<PlayerId>();
            }

            return new CustomEventHeader((byte)eventCode, sender, peers, relayMode);
        }
        else
        {
            return new CustomEventHeader((byte)eventCode, sender, null, relayMode);
        }
    }
}
