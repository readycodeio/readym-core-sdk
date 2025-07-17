using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public class HotSwappableRelayClient : IRelayClient
{
    private IRelayClient? _client;

    public event Action<IRelayClient>? OnRelayClientAttach;
    public event Action<IRelayClient>? OnRelayClientDetach;
    
    public bool IsAttached
        => _client != null;

    public void Attach(IRelayClient client)
    {
        if (_client != null)
        {
            if (_client.Connected)
                throw new InvalidOperationException("Cannot swap RelayClient while it is connected. Please stop the client first.");
                
            OnRelayClientDetach?.Invoke(_client);
            DetachRelayClient(_client);
        }
        _client = client;

        AttachRelayClient(_client);
        OnRelayClientAttach?.Invoke(_client);
    }

    public void Detach()
    {
        if (_client != null)
        {
            if (_client.Connected)
                throw new InvalidOperationException("Cannot swap RelayClient while it is connected. Please stop the client first.");
                
            OnRelayClientDetach?.Invoke(_client);
            DetachRelayClient(_client);
        }

        _client = null;
        
        _detachedOtherPlayers.Clear();
        _detachedRoomState.Clear();
        _detachedLocalPlayer.Properties.Clear();
    }

    private void AttachRelayClient(IRelayClient client)
    {
        client.OnBeforeStart += OnBeforeStartHandler;
        client.OnAfterStart += OnAfterStartHandler;
        client.OnBeforeStop += OnBeforeStopHandler;
        client.OnAfterStop += OnAfterStopHandler;
        client.OnPeerIdAssigned += OnPeerIdAssignedHandler;
        client.OnRoomPropertiesChanged += OnRoomPropertiesChangedHandler;
        client.OnPlayerPropertiesChanged += OnPlayerPropertiesChangedHandler;
        client.OnPlayerPropertiesAdded += OnPlayerPropertiesAddedHandler;
        client.OnBeforeJoinedRoom += OnBeforeJoinedRoomHandler;
        client.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
        client.OnDisconnected += OnDisconnectedHandler;
        client.OnPingUpdated += OnPingUpdatedHandler;
        client.OnEcsSnapshot += OnEcsSnapshotHandler;
        client.OnEcsDelta += OnEcsDeltaHandler;
        client.OnReceivedDeleteEntity += OnReceivedDeleteEntityHandler;
        client.OnOtherPlayerJoined += OnOtherPlayerJoinedHandler;
        client.OnOtherPlayerLeft += OnOtherPlayerLeftHandler;
        client.OnEnterRoomRequest += OnEnterRoomRequestHandler;
        client.OnExitRoomRequest += OnExitRoomRequestHandler;
        client[(byte)SystemEvent.MinCustomEvent, (byte)SystemEvent.MaxCustomEvent].OnCustomEvent += OnCustomEventHandler;
    }

    private void DetachRelayClient(IRelayClient client)
    {
        client[(byte)SystemEvent.MinCustomEvent, (byte)SystemEvent.MaxCustomEvent].OnCustomEvent -= OnCustomEventHandler;
        client.OnExitRoomRequest -= OnExitRoomRequestHandler;
        client.OnEnterRoomRequest -= OnEnterRoomRequestHandler;
        client.OnOtherPlayerLeft -= OnOtherPlayerLeftHandler;
        client.OnOtherPlayerJoined -= OnOtherPlayerJoinedHandler;
        client.OnReceivedDeleteEntity -= OnReceivedDeleteEntityHandler;
        client.OnEcsDelta -= OnEcsDeltaHandler;
        client.OnEcsSnapshot -= OnEcsSnapshotHandler;
        client.OnPingUpdated -= OnPingUpdatedHandler;
        client.OnDisconnected -= OnDisconnectedHandler;
        client.OnAfterJoinedRoom -= OnAfterJoinedRoomHandler;
        client.OnBeforeJoinedRoom -= OnBeforeJoinedRoomHandler;
        client.OnPlayerPropertiesAdded -= OnPlayerPropertiesAddedHandler;
        client.OnPlayerPropertiesChanged -= OnPlayerPropertiesChangedHandler;
        client.OnRoomPropertiesChanged -= OnRoomPropertiesChangedHandler;
        client.OnPeerIdAssigned -= OnPeerIdAssignedHandler;
        client.OnAfterStop -= OnAfterStopHandler;
        client.OnBeforeStop -= OnBeforeStopHandler;
        client.OnAfterStart -= OnAfterStartHandler;
        client.OnBeforeStart -= OnBeforeStartHandler;
    }

    public Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default)
        => _client!.UploadBlobAsync(blob, ct) ?? Task.FromResult(false);

    public Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
        => _client!.DownloadBlobAsync(name, ct) ?? Task.FromResult<BlobInfo?>(null);

    public void Dispose()
    {
        // empty
    }

    private readonly Dictionary<object, object> _detachedRoomState = new();
    private readonly Player _detachedLocalPlayer = new(new Dictionary<object, object>());
    private readonly ConcurrentDictionary<PlayerId, Player> _detachedOtherPlayers = new();

    public Dictionary<object, object> RoomState
        => _client?.RoomState ?? _detachedRoomState;

    public Player LocalPlayer
        => _client?.LocalPlayer ?? _detachedLocalPlayer;

    public ConcurrentDictionary<PlayerId, Player> OtherPlayers
        => _client?.OtherPlayers ?? _detachedOtherPlayers;

    public bool IsRunning
        => _client?.IsRunning ?? false;

    public bool Connected
        => _client?.Connected ?? false;

    public bool InRoom
        => _client?.InRoom ?? false;

    public PlayerId PlayerId
        => _client?.PlayerId ?? PlayerId.Invalid;

    public bool IsMasterClient
        => _client?.IsMasterClient ?? false;

    public event Action? OnBeforeStart;
    public event Action? OnAfterStart;
    public event Action? OnBeforeStop;
    public event Action? OnAfterStop;
    public event Action<PlayerId, Dictionary<object, object>>? OnPeerIdAssigned;
    public event Action<Dictionary<object, object?>>? OnRoomPropertiesChanged;
    public event Action<PlayerId, Dictionary<object, object?>>? OnPlayerPropertiesChanged;
    public event Action<PlayerId, Dictionary<object, object?>>? OnPlayerPropertiesAdded;
    public event Action? OnBeforeJoinedRoom;
    public event Action<Dictionary<object, object>>? OnAfterJoinedRoom;
    public event Action<DisconnectReason>? OnDisconnected;
    public event Action<int>? OnPingUpdated;
    public event Action<NetDataReader>? OnEcsSnapshot;
    public event Action<NetDataReader>? OnEcsDelta;
    public event Action<NetworkIdComponent>? OnReceivedDeleteEntity;
    public event Action<PlayerId, Dictionary<object, object>>? OnOtherPlayerJoined;
    public event Action<PlayerId>? OnOtherPlayerLeft;
    
    public event Action<CustomEventHeader, NetDataReader>? OnCustomEvent
    {
        add => this[(byte)SystemEvent.MinCustomEvent, (byte)SystemEvent.MaxCustomEvent].OnCustomEvent += value;
        remove => this[(byte)SystemEvent.MinCustomEvent, (byte)SystemEvent.MaxCustomEvent].OnCustomEvent -= value;
    }

    public CustomEventEntry this[byte minEventCode, byte maxEventCode] => new(this, minEventCode, maxEventCode);

    private readonly Action<CustomEventHeader, NetDataReader>?[] _customEventHandlers =
        new Action<CustomEventHeader, NetDataReader>?[(int)SystemEvent.MaxCustomEvent + 1];
    
    public void AddCustomEventHandler(int eventCode, Action<CustomEventHeader, NetDataReader>? value)
    {
        _customEventHandlers[eventCode] = (Action<CustomEventHeader, NetDataReader>?)Delegate.Combine(_customEventHandlers[eventCode], value);
    }

    public void RemoveCustomEventHandler(int eventCode, Action<CustomEventHeader, NetDataReader>? value)
    {
        _customEventHandlers[eventCode] = (Action<CustomEventHeader, NetDataReader>?)Delegate.Remove(_customEventHandlers[eventCode], value);
    }

    private readonly Action<ServerRpcEventHeader, NetDataReader>?[] _serverRpcEventHandlers =
        new Action<ServerRpcEventHeader, NetDataReader>?[(int)SystemEvent.MaxServerRpcEvent + 1];

    public void AddServerRpcEventHandler(ServerRpcEventEntry eventEntry, Action<ServerRpcEventHeader, NetDataReader>? value)
    {
        _serverRpcEventHandlers[eventEntry.EventCode] = (Action<ServerRpcEventHeader, NetDataReader>?)Delegate.Combine(_serverRpcEventHandlers[eventEntry.EventCode], value);
    }

    public void RemoveServerRpcEventHandler(ServerRpcEventEntry eventEntry, Action<ServerRpcEventHeader, NetDataReader>? value)
    {
        _serverRpcEventHandlers[eventEntry.EventCode] = (Action<ServerRpcEventHeader, NetDataReader>?)Delegate.Remove(_serverRpcEventHandlers[eventEntry.EventCode], value);
    }

    public event Action? OnEnterRoomRequest;
    public event Action? OnExitRoomRequest;

    public int GetMaxPacketSize(DeliveryMethod deliveryMethod)
        => _client?.GetMaxPacketSize(deliveryMethod) ?? 1300;

    public void Start()
        => _client!.Start();

    public void Stop()
        => _client!.Stop();

    public Player? GetPlayerState(PlayerId playerId)
        => _client!.GetPlayerState(playerId);

    public void SendMessageToServer(NetDataWriter writer, DeliveryMethod deliveryMethod)
        => _client!.SendMessageToServer(writer, deliveryMethod);

    public void OpSetCustomPropertiesOfActor(PlayerId playerId, Dictionary<object, object?> data)
        => _client!.OpSetCustomPropertiesOfActor(playerId, data);

    public void OpSetCustomPropertiesOfRoom(Dictionary<object, object?> data)
        => _client!.OpSetCustomPropertiesOfRoom(data);

    public void OpRaiseEvent(byte eventCode, object? data, PlayerId[] peers, DeliveryMethod deliveryMethod)
        => _client!.OpRaiseEvent(eventCode, data, peers, deliveryMethod);

    public void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod)
        => _client!.OpRaiseEvent(eventCode, data, mode, deliveryMethod);

    public void OpRaiseEvent(byte eventCode, object? data, EventCaching eventCaching)
        => _client!.OpRaiseEvent(eventCode, data, eventCaching);

    public void OpRaiseEventRaw(NetDataWriter writer, DeliveryMethod deliveryMethod)
        => _client!.OpRaiseEventRaw(writer, deliveryMethod);

    public void SendInitialPlayerState()
        => _client!.SendInitialPlayerState();

    public void EnterRoom()
        => _client!.EnterRoom();

    public void ExitRoom()
        => _client!.ExitRoom();
    
    #region Event handlers
    
    private void OnBeforeStartHandler()
        => OnBeforeStart?.Invoke();
    
    private void OnAfterStartHandler()
        => OnAfterStart?.Invoke();
    
    private void OnBeforeStopHandler()
        => OnBeforeStop?.Invoke();
    
    private void OnAfterStopHandler()
        => OnAfterStop?.Invoke();
    
    private void OnPeerIdAssignedHandler(PlayerId playerId, Dictionary<object, object> properties)
        => OnPeerIdAssigned?.Invoke(playerId, properties);
    
    private void OnRoomPropertiesChangedHandler(Dictionary<object, object?> properties)
        => OnRoomPropertiesChanged?.Invoke(properties);
    
    private void OnPlayerPropertiesChangedHandler(PlayerId playerId, Dictionary<object, object?> properties)
        => OnPlayerPropertiesChanged?.Invoke(playerId, properties);
    
    private void OnPlayerPropertiesAddedHandler(PlayerId playerId, Dictionary<object, object?> properties)
        => OnPlayerPropertiesAdded?.Invoke(playerId, properties);
    
    private void OnBeforeJoinedRoomHandler()
        => OnBeforeJoinedRoom?.Invoke();
    
    private void OnAfterJoinedRoomHandler(Dictionary<object, object> properties)
        => OnAfterJoinedRoom?.Invoke(properties);
    
    private void OnDisconnectedHandler(DisconnectReason reason)
        => OnDisconnected?.Invoke(reason);
    
    private void OnPingUpdatedHandler(int ping)
        => OnPingUpdated?.Invoke(ping);
    
    private void OnEcsSnapshotHandler(NetDataReader reader)
        => OnEcsSnapshot?.Invoke(reader);
    
    private void OnEcsDeltaHandler(NetDataReader reader)
        => OnEcsDelta?.Invoke(reader);
    
    private void OnReceivedDeleteEntityHandler(NetworkIdComponent networkId)
        => OnReceivedDeleteEntity?.Invoke(networkId);
    
    private void OnOtherPlayerJoinedHandler(PlayerId playerId, Dictionary<object, object> properties)
        => OnOtherPlayerJoined?.Invoke(playerId, properties);
    
    private void OnOtherPlayerLeftHandler(PlayerId playerId)
        => OnOtherPlayerLeft?.Invoke(playerId);
    
    private void OnEnterRoomRequestHandler()
        => OnEnterRoomRequest?.Invoke();
    
    private void OnExitRoomRequestHandler()
        => OnExitRoomRequest?.Invoke();
        
    private void OnCustomEventHandler(CustomEventHeader ev, NetDataReader reader)
    {
        var customEventHandler = _customEventHandlers[ev.EventCode];
        customEventHandler?.Invoke(ev, reader);
    }

    #endregion
}
