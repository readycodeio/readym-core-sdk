using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Client;

public struct RelayMessage
{
    internal readonly RelayMessageCode EventCode;
    public readonly NetDataWriter Writer;
    internal readonly PlayerId[]? Peers;
    internal readonly RelayMode Mode;
    internal readonly DeliveryMethod DeliveryMethod;

    private RelayMessage(
        RelayMessageCode eventCode,
        NetDataWriter writer,
        PlayerId[]? peers,
        RelayMode mode,
        DeliveryMethod deliveryMethod)
    {
        EventCode = eventCode;
        Writer = writer;
        Peers = peers;
        Mode = mode;
        DeliveryMethod = deliveryMethod;
    }

    internal static RelayMessage ToServer(RelayMessageCode eventCode, DeliveryMethod deliveryMethod)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)eventCode);
        return new RelayMessage(
            eventCode,
            writer,
            null,
            default,
            deliveryMethod
        );
    }

    /// <summary>
    /// Send an event to a specific player or group of players.
    /// This overload does not support event caching, as cached events must either be sent to all other players or all players.
    /// </summary>
    internal static RelayMessage ToPeers(RelayMessageCode eventCode, PlayerId playerId, PlayerId[] peers, DeliveryMethod deliveryMethod)
    {
        var writer = new NetDataWriter();
        writer.PutCustomRelayEventHeader(eventCode, playerId, peers);
        return new RelayMessage(
            eventCode,
            writer,
            peers,
            RelayMode.Peers,
            deliveryMethod
        );
    }
    
    /// <summary>
    /// Send an event with a specific delivery method. This overload does not support event caching.
    /// </summary>
    public static RelayMessage ByRelayMode(RelayMessageCode eventCode, PlayerId playerId, RelayMode mode, DeliveryMethod deliveryMethod)
    {
        var writer = new NetDataWriter();
        writer.PutCustomRelayEventHeader(eventCode, playerId, mode);
        return new RelayMessage(
            eventCode,
            writer,
            null,
            mode,
            deliveryMethod
        );
    }
}