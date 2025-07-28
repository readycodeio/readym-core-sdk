using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Common.Protocol;

namespace ReadyM.Relay.Client;

public class RelayClient : IShimRecordableRelayClient
{
    private class NetworkThreadContext : IRelayClientNetworkThreadContext
    {
        public readonly List<PlayerId> AllPlayers = new();
        public readonly List<PlayerId> AreaPlayers = new();

        public bool Connected { get; set; }
        public PlayerId PlayerId { get; set; }
        public AreaId CurrentArea { get; set; }
        
        public DisconnectReason LastDisconnectReason { get; set; }

        ReadOnlyList<PlayerId> IRelayClientNetworkThreadContext.AllPlayers
            => new(AllPlayers);
        
        ReadOnlyList<PlayerId> IRelayClientNetworkThreadContext.AreaPlayers
            => new(AreaPlayers);
    }
    
    // Proper implementations guaranteed to be thread-safe
    private readonly ILogger _logger;
    
    // Looking at the implementation it seems to be thread-safe for reading properties. Since it is accessed from
    // multiple threads, all properties should be assumed to be volatile, e.g. a list of peers may change between
    // one iteration and the next on the same thread.
    private readonly NetManager _client;
    
    // Only used to subscribe to events, only ever used on the main thread.
    private readonly EventBasedNetListener _listener;
    
    // Read-only value types, so thread safe
    private readonly RelayConnectionOptions _options;
    private readonly string _host;
    private readonly int _port;

    // This isn't thread-safe, but we use it for some inconsequential things. DO NOT USE it for anything important,
    // it'll return an abnormal number of 0s when used in parallel. With the papal blessing nothing should break
    // because of it.
    private readonly Random _rng = new(2137);

    // NOTE: Stores data that can only be safely accessed from the network thread. It is disallowed to access any of 
    // this state from other threads.
    private readonly NetworkThreadContext _netThreadContext = new();

    private readonly PendingActionUpdater<IRelayClientNetworkThreadContext> _scheduler;
    
    // NOTE: This gets assigned early on inside the `OnConnected` event handler. From that point on it is 
    // readonly and immutable until the client disconnects. Since `PlayerId` for the client needs to be available
    // for read from both the main thread the network thread, we have to make this field guarded by a memory barrier.
    // The implicit memory barrier inside `_connectedSignal` is used in this case. Hence, it is only permissible to
    // read from the property once the `Set()` method has been called on `_connectedSignal`.
    // NOTE: It is important for thread safety of this approach that reconnects without the full `Stop()` and `Start()`
    // cycle do not change the assigned `PlayerId` to a different value.
    public PlayerId PlayerId
    {
        get
        {
            var playerId = _netThreadContext.PlayerId;
            if (playerId == default || !IsRunning)
                throw new InvalidOperationException(
                    "PlayerId is not set. You need to call `Start()` and wait on the returned task before safely reading this property.");
            return playerId;
        }
    }
    
    // NOTE: There is no `Connected` property because there is no conceivable way that could make reading it thread-safe.
    // Connection can be dropped at any time. Hence, if such property existed, reading from it on the main thread
    // would introduce a race condition each time.

    public DisconnectReason LastDisconnectReason
    {
        get
        {
            if (IsRunning)
                throw new InvalidOperationException("Call `Stop()` before safely reading this field.");
            return _netThreadContext.LastDisconnectReason;
        }
    }
    
    // Only changed from the main thread. Not affected by disconnections.
    public bool IsRunning { get; private set; }

    public event Action? OnRequestedStart;
    public event Action? OnRequestedStop;

    public event Action? OnRequestedConnect;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnConnected;
    public event Action? OnRequestedDisconnect;
    public event Action<IRelayClientNetworkThreadContext, DisconnectReason>? OnDisconnected;
    
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerConnected;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerDisconnected;
    public event Action<AreaId>? OnRequestedJoinArea;
    public event Action<IRelayClientNetworkThreadContext, AreaId>? OnJoinedArea;
    public event Action? OnRequestedLeaveArea;
    public event Action<IRelayClientNetworkThreadContext>? OnLeftArea;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerJoinedArea;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerLeftArea;

    public event Action<IRelayClientNetworkThreadContext, int>? OnPingUpdated;

    public event Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader>? OnAnyMessage
    {
        add => AddMessageHandler(RelayMessageCode.MinCustomEvent, RelayMessageCode.MaxCustomEvent, value!);
        remove => RemoveMessageHandler(RelayMessageCode.MinCustomEvent, RelayMessageCode.MaxCustomEvent, value!);
    }

    private readonly Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader>?[] _messageHandlers =
        new Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader>?[(int)RelayMessageCode.MaxCustomEvent + 1];
    
    public void AddMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader> handler)
    {
        _messageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader>?)Delegate.Combine(_messageHandlers[(byte)eventCode], handler);
    }

    public void AddMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader> handler)
    {
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _messageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader>?)Delegate.Combine(_messageHandlers[(byte)i], handler);
        }
    }

    public void RemoveMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader> handler)
    {
        _messageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader>?)Delegate.Remove(_messageHandlers[(byte)eventCode], handler);
    }

    public void RemoveMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader> handler)
    {
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _messageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader>?)Delegate.Remove(_messageHandlers[(byte)i], handler);
        }
    }

    public event Action<IRelayClientNetworkThreadContext>? OnClientUpdate;

    public NetPeer? Server
    {
        get
        {
            if (_client.FirstPeer == null)
            {
                _logger.LogError("Disconnected from server");
            }

            return _client.FirstPeer;
        }
    }

    public PendingActionScheduler<IRelayClientNetworkThreadContext> Scheduler
    {
        get
        {
            if (!IsRunning)
                throw new InvalidOperationException("Relay client is not running, cannot access scheduler");
            return _scheduler;
        }
    }

    public RelayClient(string host, int port, RelayConnectionOptions options, ILogger logger) 
    {
        _logger = logger;
        
        _options = options;
        _host = host;
        _port = port;
        _scheduler = new(_netThreadContext, _logger);

        _listener = new EventBasedNetListener();
        _listener.NetworkReceiveEvent += OnListenerNetworkReceiveEvent;
        _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;
        _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;

        _client = new NetManager(_listener)
        {
            AutoRecycle = true,
            EnableStatistics = true,
#if NO_DISCONNECT
            DisconnectTimeout = 3600_000,
            PingInterval = 3600_000,
            DisconnectOnUnreachable = false,
#else
            DisconnectOnUnreachable = true,
#endif
            UpdateTime = Constants.ClientNetworkTickRateMs,
        };
    }

    public void Dispose()
    {
        if (IsRunning)
        {
            Stop();
        }
        
        _listener.PeerDisconnectedEvent -= OnPeerDisconnectedEvent;
        _listener.NetworkLatencyUpdateEvent -= OnNetworkLatencyUpdateEvent;
        _listener.NetworkReceiveEvent -= OnListenerNetworkReceiveEvent;
    }

    public int GetMaxPacketSize(DeliveryMethod deliveryMethod)
    {
        return Server?.GetMaxSinglePacketSize(deliveryMethod) ?? 1300;
    }

    public async Task StartAsync(CancellationToken token, bool autoConnect = true)
    {
        if (IsRunning)
        {
            _logger.LogError("Relay client is already running");
            return;
        }

        OnRequestedStart?.Invoke();

        await Task.Delay(1, token);
        _scheduler.SetThread(Thread.CurrentThread);

        _logger.LogDebug("Starting on {Host}:{Port}", _host, _port);
        _client.Start();

        var writer = new NetDataWriter();
        _options.Serialize(writer);
        
        if (autoConnect)
        {
            _logger.LogInformation("Connecting to {Host}:{Port}", _host, _port);
            _client.Connect(_host, _port, writer);
            _logger.LogInformation("Connected to {Host}:{Port}", _host, _port);
        }
        
        _logger.LogInformation("Running on {Host}:{Port}", _host, _port);
        
        IsRunning = true;

        while (!token.IsCancellationRequested)
        {
            try
            {
                _client.PollEvents();
                
                if (_netThreadContext.Connected)
                    break;

                await Task.Delay(Constants.ClientNetworkTickRateMs, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in client thread (starting)");
            }
        }
        
        _logger.LogDebug("Started on {Host}:{Port}", _host, _port);
    }

    public async Task RunAsync(CancellationToken token)
    {
        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }
        
        while (!token.IsCancellationRequested)
        {
            try
            {
                _client.PollEvents();
                
                OnClientUpdate?.Invoke(_netThreadContext);

                var hadPendingActions = _scheduler.Update();
                if (!hadPendingActions)
                {
                    await Task.Delay(Constants.ClientNetworkTickRateMs, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in client thread");
            }
        }
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }
        
        IsRunning = false;

        _logger.LogDebug("Stopping on {Host}:{Port}", _host, _port);

        OnRequestedStop?.Invoke();

        // NOTE: `OnDisconnected` will be called by LiteNetLib when the client is disconnected.
        _client.Stop();

        // NOTE: It is possible that the client requests a disconnect, and simultaneously the server disconnects
        // from the client forcefully. In that case the corresponding `OnDisconnected` event will not be fired.
        if (LastDisconnectReason != DisconnectReason.DisconnectPeerCalled)
        {
            _logger.LogWarning("Already disconnected: {Reason}", LastDisconnectReason);
        }

        _logger.LogDebug("Stopped on {Host}:{Port}", _host, _port);
    }

    public void Connect()
    {
        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }
        
        OnRequestedConnect?.Invoke();
        
        var writer = new NetDataWriter();
        _options.Serialize(writer);
        
        _client.Connect(_host, _port, writer);
        _logger.LogInformation("Explicitly connecting on {Host}:{Port}", _host, _port);
    }

    public void Disconnect()
    {
        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }

        _logger.LogInformation("Explicitly disconnecting from {Host}:{Port}", _host, _port);

        OnRequestedDisconnect?.Invoke();
        
        _client.DisconnectAll();
    }

    public void Reconnect()
    {
        Disconnect();
        Connect();
    }

    public void JoinArea(AreaId areaId)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)RelayMessageCode.RequestAreaEvent);
        var playerId = PlayerId;
        writer.Put(playerId);
        writer.Put(true); // Request joining area event
        areaId.Serialize(writer);
        SendRawMessage(writer, DeliveryMethod.ReliableOrdered);
    }

    public void LeaveArea()
    {
        var writer = new NetDataWriter();
        writer.Put((byte)RelayMessageCode.RequestAreaEvent);
        var playerId = PlayerId;
        writer.Put(playerId);
        writer.Put(false); // Request leaving area event
        SendRawMessage(writer, DeliveryMethod.ReliableOrdered);
    }

    public void SendRawMessage(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        Server?.Send(writer, deliveryMethod);
        var ev = writer.Data[0];
        AppendToSentStats((RelayMessageCode)ev, writer.Length);
    }
    
    public void SendMessage(RelayMessage message)
    {
        Server?.Send(message.Writer, message.DeliveryMethod);
        AppendToSentStats(message.EventCode, message.Writer.Length);
    }

    public void SendMessageToServer<T>(RelayMessageCode eventCode, T data, DeliveryMethod deliveryMethod) where T : INetSerializable
    {
        var message = RelayMessage.ToServer(eventCode, deliveryMethod);
        data.Serialize(message.Writer);
        SendMessage(message);
    }

    public void SendMessageToPeers<T>(RelayMessageCode eventCode, T data, PlayerId[] peers, DeliveryMethod deliveryMethod)
        where T : INetSerializable
    {
        var message = RelayMessage.ToPeers(eventCode, PlayerId, peers, deliveryMethod);
        data.Serialize(message.Writer);
        SendMessage(message);
    }

    public void SendMessageRelayMode<T>(RelayMessageCode eventCode, T data, RelayMode mode, DeliveryMethod deliveryMethod)
        where T : INetSerializable
    {
        var message = RelayMessage.ByRelayMode(eventCode, PlayerId, mode, deliveryMethod);
        SendMessage(message);
    }

    private readonly ConcurrentDictionary<RelayMessageCode, (long Count, long Bytes)> _statsSent = new();
    private readonly ConcurrentDictionary<RelayMessageCode, (long Count, long Bytes)> _statsRecv = new();

    private void AppendToSentStats(RelayMessageCode ev, long bytesSent)
    {
        _statsSent.AddOrUpdate(ev, (1, bytesSent), (_, data) => (data.Count + 1, data.Bytes + bytesSent));
    }

    private void AppendToRecvStats(RelayMessageCode ev, long bytesRecv)
    {
        _statsRecv.AddOrUpdate(ev, (1, bytesRecv), (_, data) => (data.Count + 1, data.Bytes + bytesRecv));
    }

    private void OnListenerNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
    {
        var eventCode = (RelayMessageCode)reader.GetByte();
        AppendToRecvStats(eventCode, reader.UserDataSize);

        switch (eventCode)
        {
            case RelayMessageCode.HandshakeConnected:
            {
                var playerId = reader.Get<PlayerId>();
                if (_netThreadContext.PlayerId != PlayerId.Invalid)
                {
                    _logger.LogError("Missing handshake for player {PlayerId} but already assigned {AssignedPlayerId}", playerId, _netThreadContext.PlayerId);
                }
                _netThreadContext.Connected = true;
                _netThreadContext.PlayerId = playerId;
                _netThreadContext.AllPlayers.Add(playerId);
                _logger.LogInformation("Assigned Actor ID {PlayerId}", playerId);
                OnConnected?.Invoke(_netThreadContext, playerId);
                break;
            }
            case RelayMessageCode.AreaEvent:
            {
                var playerId = reader.Get<PlayerId>();
                var isJoining = reader.GetBool();
                if (isJoining)
                {
                    if (_netThreadContext.PlayerId == PlayerId.Invalid)
                    {
                        _logger.LogError("Received handshake for joining area {AreaId} by player {PlayerId} but PlayerId is not set", playerId, _netThreadContext.PlayerId);
                        break;
                    }
                    if (playerId != PlayerId)
                    {
                        _logger.LogError("Received handshake for player {PlayerId} but expected {ExpectedPlayerId}", playerId, PlayerId);
                        break;
                    }

                    if (_netThreadContext.CurrentArea != AreaId.Invalid)
                    {
                        _logger.LogError("Received handshake for joining area {AreaId} by player {PlayerId} but already in area {CurrentArea}", playerId, _netThreadContext.PlayerId, _netThreadContext.CurrentArea);
                        break;
                    }
                    AreaId areaId = default;
                    areaId.Deserialize(reader);
                    _netThreadContext.CurrentArea = areaId;
                    _netThreadContext.AreaPlayers.Clear();
                    _netThreadContext.AreaPlayers.Add(playerId);
                    OnJoinedArea?.Invoke(_netThreadContext, areaId);
                }
                else
                {
                    if (_netThreadContext.PlayerId == PlayerId.Invalid)
                    {
                        _logger.LogError("Received handshake for leaving area by player {PlayerId} but PlayerId is not set", playerId);
                        break;
                    }
                    if (playerId != PlayerId)
                    {
                        _logger.LogError("Received handshake for player {PlayerId} but expected {ExpectedPlayerId}", playerId, PlayerId);
                        break;
                    }

                    if (_netThreadContext.CurrentArea == AreaId.Invalid)
                    {
                        _logger.LogError("Received handshake for leaving area by player {PlayerId} but not in any area", playerId);
                        break;
                    }
                    
                    _netThreadContext.CurrentArea = AreaId.Invalid;
                    _netThreadContext.AreaPlayers.Remove(playerId);
                    OnLeftArea?.Invoke(_netThreadContext);
                }
                break;
            }
            case RelayMessageCode.OtherPlayerConnectionEvent:
            {
                var playerId = reader.Get<PlayerId>();
                var isConnecting = reader.GetBool();
                if (isConnecting)
                {
                    if (!_netThreadContext.AllPlayers.Contains(playerId))
                    {
                        _netThreadContext.AllPlayers.Add(playerId);
                        OnOtherPlayerConnected?.Invoke(_netThreadContext, playerId);
                    }
                    else
                    {
                        _logger.LogError("Player connected event for player {PlayerId} that already is marked as connected", playerId);
                    }
                }
                else
                {
                    if (_netThreadContext.AllPlayers.Contains(playerId))
                    {
                        _netThreadContext.AllPlayers.Remove(playerId);
                        if (_netThreadContext.AreaPlayers.Contains(playerId))
                        {
                            _logger.LogInformation("Player disconnected event for player {PlayerId} that is still in the area", playerId);
                            _netThreadContext.AreaPlayers.Remove(playerId);
                            OnOtherPlayerLeftArea?.Invoke(_netThreadContext, playerId);
                        }
                        OnOtherPlayerDisconnected?.Invoke(_netThreadContext, playerId);
                    }
                    else
                    {
                        _logger.LogError("Player disconnected event for player {PlayerId} that already is marked as NOT connected", playerId);
                    }
                }
                break;
            }
            case RelayMessageCode.OtherPlayerAreaEvent:
            {
                var playerId = reader.Get<PlayerId>();
                var isJoining = reader.GetBool();
                if (isJoining)
                {
                    if (_netThreadContext.CurrentArea == AreaId.Invalid)
                    {
                        _logger.LogError("Received area event for player {PlayerId} but current area is not set", playerId);
                        break;
                    }
                    if (!_netThreadContext.AreaPlayers.Contains(playerId))
                    {
                        _netThreadContext.AreaPlayers.Add(playerId);
                        OnOtherPlayerJoinedArea?.Invoke(_netThreadContext, playerId);
                    }
                    else
                    {
                        _logger.LogError("Player joined area event for player {PlayerId} that already is marked as in the area", playerId);
                    }
                }
                else
                {
                    if (_netThreadContext.AreaPlayers.Contains(playerId))
                    {
                        _netThreadContext.AreaPlayers.Remove(playerId);
                        OnOtherPlayerLeftArea?.Invoke(_netThreadContext, playerId);
                    }
                    else
                    {
                        _logger.LogError("Player left area event for player {PlayerId} that already is marked as NOT in the area", playerId);
                    }
                }
                break;
            }
            default:
            {
                var header = reader.GetCustomEventHeader(eventCode);
                var eventHandler = _messageHandlers[(byte)eventCode];
                eventHandler?.Invoke(_netThreadContext, header, reader);
                break;
            }
        }
    }

    private readonly object _statLock = new();
    private long _lastBytesReceived;
    private long _lastBytesSent;
    private DateTimeOffset _lastStatCheck = DateTimeOffset.Now;

    private void OnNetworkLatencyUpdateEvent(NetPeer peer, int latency)
    {
        // Round trip time. LiteNetLib reports one way latency, so we double it.
        // We add a random jitter so that the results are not always divisible by 2.
        OnPingUpdated?.Invoke(_netThreadContext, 2 * latency + _rng.Next(2));

        // Print stats every time too
        
        // NOTE: We need to read this once so that it is atomic
        var bytesReceived = _client.Statistics.BytesReceived;
        var bytesSent = _client.Statistics.BytesSent;

        long dRecv;
        long dSent;
        TimeSpan delta;
        
        // NOTE: There's no atomic way of assigning DateTimeOffset which is wider than 8 bytes and therefore
        // cannot rely on atomicity of single assignments. Therefore we opt in for an explicit lock here.
        lock (_statLock)
        {
            dRecv = bytesReceived - _lastBytesReceived;
            _lastBytesReceived = bytesReceived;

            dSent = bytesSent - _lastBytesSent;
            _lastBytesSent = bytesSent;

            var now = DateTimeOffset.Now;
            delta = now - _lastStatCheck;

            _lastStatCheck = now;
        }

        // print avg recv and sent over the delta time
        var avgRecv = (long)(dRecv / delta.TotalSeconds);
        var avgSent = (long)(dSent / delta.TotalSeconds);

        _logger.LogDebug("Avg recv: {Recv} B/s, Avg sent: {Sent} B/s", avgRecv, avgSent);
        LogEventStats();
    }
    
    private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo info)
    {
        _logger.LogInformation("Disconnected from server: {Reason}", info.Reason);
        
        var playerId = _netThreadContext.PlayerId;

        if (_netThreadContext.CurrentArea != AreaId.Invalid)
        {
            if (_netThreadContext.AreaPlayers.Contains(playerId))
            {
                _netThreadContext.AreaPlayers.Remove(playerId);
            }
            _netThreadContext.CurrentArea = AreaId.Invalid;
            OnLeftArea?.Invoke(_netThreadContext);
        }

        _netThreadContext.Connected = false;
        _netThreadContext.AllPlayers.Clear();
        _netThreadContext.AreaPlayers.Clear();
        _netThreadContext.LastDisconnectReason = info.Reason;
        // NOTE: `PlayerId` is not reset! Changing `PlayerId` here would introduce race conditions for the users of
        // this property on the main thread.
        OnDisconnected?.Invoke(_netThreadContext, info.Reason);
    }
    
    private void LogEventStats()
    {
#if DEBUG
        foreach (var kvp in _statsSent.OrderByDescending(x => x.Value))
        {
            _logger.LogTrace("Event {Event}: sent {Bytes} B, avg {Average} B", kvp.Key, kvp.Value.Bytes, kvp.Value.Bytes / kvp.Value.Count);
        }

        _logger.LogTrace("----------------------------------------");
        foreach (var kvp in _statsRecv.OrderByDescending(x => x.Value))
        {
            _logger.LogTrace("Event {Event}: recv {Bytes} B, avg {Average} B", kvp.Key, kvp.Value.Bytes, kvp.Value.Bytes / kvp.Value.Count);
        }
#endif
    }
}
