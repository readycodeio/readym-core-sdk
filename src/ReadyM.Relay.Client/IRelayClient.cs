using System;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Common.Serialization;

namespace ReadyM.Relay.Client;

public interface IRelayClient
{
    bool InRoom { get; }
    event Action<DisconnectReason>? OnDisconnected;
    event Action<int>? OnPingUpdated;
    event Action<CustomEventHeader, NetPacketReader>? OnCustomEvent;
    void Start();
    void Stop();

    /// <summary>
    /// Send an event to a specific player or group of players.
    /// This overload does not support event caching, as cached events must either be sent to all other players or all players.
    /// </summary>
    void OpRaiseEvent(byte eventCode, object? data, int[] peers, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Send an event with a specific delivery method. This overload does not support event caching.
    /// </summary>
    void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Send an event that will be cached by the server and sent to all/other players (depending on the eventCaching parameter).
    /// </summary>
    void OpRaiseEvent(byte eventCode, object? data, EventCaching eventCaching);

    void OpRaiseEventRaw(NetDataWriter writer, DeliveryMethod deliveryMethod);

    byte RegisterType(
        Type customType,
        SerializeMethod serializeMethod,
        DeserializeMethod deserializeMethod);
}