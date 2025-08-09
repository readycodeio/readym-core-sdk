using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Extensions;

public static class NetDataWriterExtensions
{
    public static void PutCustomRelayEventHeader(this NetDataWriter writer, RelayMessageCode eventCode, PlayerId playerId, RelayMode relayMode)
    {
        if (relayMode == RelayMode.Peers)
            throw new ArgumentException("Use PutCustomRelayEventHeader with PlayerId[] for RelayMode.Peers", nameof(relayMode));
        writer.Put((byte)eventCode);
        writer.Put(playerId);
        var flags = (byte)relayMode;
        writer.Put(flags);
    }

    public static void PutCustomRelayEventHeader(this NetDataWriter writer, RelayMessageCode eventCode, PlayerId playerId, PlayerId[] peers)
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

    public static CustomRelayEventHeader GetCustomRelayEventHeader(this NetDataReader reader, RelayMessageCode eventCode)
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

            return new CustomRelayEventHeader((byte)eventCode, sender, peers, relayMode);
        }
        else
        {
            return new CustomRelayEventHeader((byte)eventCode, sender, null, relayMode);
        }
    }

    // We do not write the sender, since the server tracks peer IDs anyway.
    public static void PutServerRpcEventHeader(this NetDataWriter writer, byte eventCode)
    {
        if (eventCode is < (byte)RelayMessageCode.MinServerRpcEvent or > (byte)RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinServerRpcEvent)}` and `{nameof(RelayMessageCode.MaxServerRpcEvent)}`");
        writer.Put(eventCode);
    }
}
