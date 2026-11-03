using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Client;

/// <exclude />
/// <summary>
/// Represents a message to be sent through the relay system.
/// The type is public because it is used in mod-generated code, but it is not intended for direct use by mod developers.
/// </summary>
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

    public static RelayMessage ToServer(RelayMessageCode eventCode, DeliveryMethod deliveryMethod)
    {
        var writer = new NetDataWriter();
        writer.PutMessageCode(eventCode);
        return new RelayMessage(
            eventCode,
            writer,
            null,
            default,
            deliveryMethod
        );
    }
    
    /// <summary>
    /// Send an event with a specific delivery method.
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