using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public interface IRelayClient : IBlobClient, IDisposable
{
    Dictionary<object, object> RoomState { get; }
    Player LocalPlayer { get; }
    ConcurrentDictionary<PlayerId, Player> OtherPlayers { get; }
    bool IsRunning { get; }
    bool Connected { get; }
    bool InRoom { get; }
    PlayerId PlayerId { get; }
    bool IsMasterClient { get; }

    event Action? OnBeforeStart;
    event Action? OnAfterStart;
    event Action? OnBeforeStop;
    event Action? OnAfterStop;
    
    event Action<PlayerId, Dictionary<object, object>>? OnPeerIdAssigned;
    event Action<Dictionary<object, object?>>? OnRoomPropertiesChanged;
    event Action<PlayerId, Dictionary<object, object?>>? OnPlayerPropertiesChanged;
    event Action<PlayerId, Dictionary<object, object?>>? OnPlayerPropertiesAdded;
    event Action? OnBeforeJoinedRoom;
    event Action<Dictionary<object, object>>? OnAfterJoinedRoom;
    event Action<DisconnectReason>? OnDisconnected;
    event Action<int>? OnPingUpdated;
    public event Action<NetDataReader>? OnEcsSnapshot;
    event Action<NetDataReader>? OnEcsDelta;
    event Action<NetworkIdComponent>? OnReceivedDeleteEntity;
    
    /// <summary>
    /// At this point the connecting player has been assigned an ID and we have synced their state.
    /// </summary>
    event Action<PlayerId, Dictionary<object, object>>? OnOtherPlayerJoined;
    event Action<PlayerId>? OnOtherPlayerLeft;
    event Action<CustomEventHeader, NetDataReader>? OnCustomEvent;
    
    public CustomEventEntry this[byte minEventCode, byte maxEventCode] { get; }

    void AddCustomEventHandler(int eventCode, Action<CustomEventHeader, NetDataReader>? value);
    void RemoveCustomEventHandler(int eventCode, Action<CustomEventHeader, NetDataReader>? value);
    
    void AddServerRpcEventHandler(ServerRpcEventEntry eventEntry, Action<ServerRpcEventHeader, NetDataReader>? value);
    void RemoveServerRpcEventHandler(ServerRpcEventEntry eventEntry, Action<ServerRpcEventHeader, NetDataReader>? value);

    event Action? OnEnterRoomRequest;
    event Action? OnExitRoomRequest;

    int GetMaxPacketSize(DeliveryMethod deliveryMethod);
    void Start();
    void Stop();
    
    Player? GetPlayerState(PlayerId playerId);
    void SendMessageToServer(NetDataWriter writer, DeliveryMethod deliveryMethod);
    void OpSetCustomPropertiesOfActor(PlayerId playerId, Dictionary<object, object?> data);
    void OpSetCustomPropertiesOfRoom(Dictionary<object, object?> data);

    /// <summary>
    /// Send an event to a specific player or group of players.
    /// This overload does not support event caching, as cached events must either be sent to all other players or all players.
    /// </summary>
    void OpRaiseEvent(byte eventCode, object? data, PlayerId[] peers, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Send an event with a specific delivery method. This overload does not support event caching.
    /// </summary>
    void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod);

    /// <summary>
    /// Send an event the will be cached by the server and sent to all/other players (depending on the eventCaching parameter).
    /// </summary>
    void OpRaiseEvent(byte eventCode, object? data, EventCaching eventCaching);

    void OpRaiseEventRaw(NetDataWriter writer, DeliveryMethod deliveryMethod);

    void SendInitialPlayerState();

    void EnterRoom();
    void ExitRoom();
}
