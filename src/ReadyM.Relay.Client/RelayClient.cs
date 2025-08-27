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

public class RelayClient : IRelayClient
{
    private class NetworkThreadContext : IRelayClientNetworkThreadContext
    {
        public readonly List<PlayerId> AllPlayers = new();
        public readonly List<PlayerId> AreaPlayers = new();

        public bool IsConnected { get; set; }
        public PlayerId? PlayerId { get; set; }
        public AreaId? CurrentAreaId { get; set; }
        
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
    public PlayerId? PlayerId
    {
        get
        {
            if (!RequestedConnect)
                return null;
            return _netThreadContext.PlayerId;
        }
    }
    
    private volatile bool _isRunning;
    private readonly ManualResetEventSlim _playerIdAssignedEvent = new();

    public bool RequestedConnect { get; private set; }
    public AreaId? RequestedAreaId { get; private set; }
    
    // NOTE: There is no `Connected` property because there is no conceivable way that could make reading it thread-safe.
    // Connection can be dropped at any time. Hence, if such property existed, reading from it on the main thread
    // would introduce a race condition each time.

    public event Action? OnStart;
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

    public event Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>? OnAnyBuiltInMessage
    {
        add => AddBuiltInMessageHandler(RelayMessageCode.MinBuiltInEvent, RelayMessageCode.MaxBuiltInEvent, value!);
        remove => RemoveBuiltInMessageHandler(RelayMessageCode.MinBuiltInEvent, RelayMessageCode.MaxBuiltInEvent, value!);
    }

    public event Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>? OnAnyServerRpcMessage
    {
        add => AddServerRpcMessageHandler(RelayMessageCode.MinServerRpcEvent, RelayMessageCode.MaxServerRpcEvent, value!);
        remove => RemoveServerRpcMessageHandler(RelayMessageCode.MinServerRpcEvent, RelayMessageCode.MaxServerRpcEvent, value!);
    }

    public event Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader>? OnAnyClientRpcMessage
    {
        add => AddClientRpcMessageHandler(RelayMessageCode.MinClientRpcEvent, RelayMessageCode.MaxClientRpcEvent, value!);
        remove => RemoveClientRpcMessageHandler(RelayMessageCode.MinClientRpcEvent, RelayMessageCode.MaxClientRpcEvent, value!);
    }

    private readonly Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?[] _serverMessageHandlers =
        new Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?[(int)RelayMessageCode.MaxBuiltInEvent + 1];
    private readonly Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader>?[] _clientMessageHandlers =
        new Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader>?[(int)RelayMessageCode.MaxClientRpcEvent + 1];

    public void AddBuiltInMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinBuiltInEvent || eventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        _serverMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)eventCode], handler);
    }

    public void AddBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinBuiltInEvent || minEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        if (maxEventCode < RelayMessageCode.MinBuiltInEvent || maxEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)i], handler);
        }
    }

    public void RemoveBuiltInMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinBuiltInEvent || eventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Remove(_serverMessageHandlers[(byte)eventCode], handler);
    }

    public void RemoveBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinBuiltInEvent || minEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        if (maxEventCode < RelayMessageCode.MinBuiltInEvent || maxEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");

        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)i], handler);
        }
    }

    public void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinServerRpcEvent || eventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinServerRpcEvent)}` and `{nameof(RelayMessageCode.MaxServerRpcEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)eventCode], handler);
    }

    public void AddServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinServerRpcEvent || minEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        if (maxEventCode < RelayMessageCode.MinServerRpcEvent || maxEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)i], handler);
        }
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinServerRpcEvent || eventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinServerRpcEvent)}` and `{nameof(RelayMessageCode.MaxServerRpcEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Remove(_serverMessageHandlers[(byte)eventCode], handler);
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinServerRpcEvent || minEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        if (maxEventCode < RelayMessageCode.MinServerRpcEvent || maxEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Remove(_serverMessageHandlers[(byte)i], handler);
        }
    }

    public void AddClientRpcMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader> handler)
    {
        if (eventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        _clientMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)eventCode], handler);
    }

    public void AddClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader> handler)
    {
        if (minEventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        if (maxEventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _clientMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)i], handler);
        }
    }

    public void RemoveClientRpcMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader> handler)
    {
        if (eventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        _clientMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader>?)Delegate.Remove(_clientMessageHandlers[(byte)eventCode], handler);
    }

    public void RemoveClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader> handler)
    {
        if (minEventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _clientMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader>?)Delegate.Remove(_clientMessageHandlers[(byte)i], handler);
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
        => _scheduler;

    public RelayClient(string host, int port, RelayConnectionOptions options, bool noDisconnect, ILogger logger) 
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
            UpdateTime = Constants.ClientNetworkTickRateMs,
        };
        
        if (noDisconnect)
        {
            _client.DisconnectTimeout = 3600_000;
            _client.PingInterval = 3600_000;
            _client.DisconnectOnUnreachable = false;
        }
        else
        {
            _client.DisconnectOnUnreachable = true;
        }
    }

    public void Dispose()
    {
        if (_isRunning)
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

    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Relay client is already running");
            return;
        }
        _isRunning = true;

        OnStart?.Invoke();

        _logger.LogDebug("Starting on {Host}:{Port}...", _host, _port);
        _client.Start();
        _logger.LogDebug("Started on {Host}:{Port}", _host, _port);
    }

    public async Task RunAsync(CancellationToken token)
    {
        if (!_isRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }

        await Task.Delay(1, token);
        _scheduler.SetThread(Thread.CurrentThread);

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
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Client loop was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in client thread");
            }
        }
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Relay client is not running");
            return;
        }
        
        _isRunning = false;
        _scheduler.SetThread(null);

        _logger.LogDebug("Stopping on {Host}:{Port}...", _host, _port);

        OnRequestedStop?.Invoke();

        // NOTE: `OnDisconnected` will be called by LiteNetLib when the client is disconnected.
        _client.DisconnectAll();
        _client.PollEvents();

        _client.Stop();
        
        // NOTE: It is possible that the client requests a disconnect, and simultaneously the server disconnects
        // from the client forcefully. In that case the corresponding `OnDisconnected` event will not be fired.
        if (_netThreadContext.LastDisconnectReason != DisconnectReason.DisconnectPeerCalled)
        {
            _logger.LogWarning("Already disconnected: {Reason}", _netThreadContext.LastDisconnectReason);
        }

        _logger.LogDebug("Stopped on {Host}:{Port}", _host, _port);
    }

    public void RequestConnect()
    {
        if (RequestedConnect)
        {
            _logger.LogWarning("Relay client is already connecting");
            return;
        }
        RequestedConnect = true;

        _logger.LogInformation("Connecting on {Host}:{Port}...", _host, _port);
        
        OnRequestedConnect?.Invoke();
        
        var writer = new NetDataWriter();
        _options.Serialize(writer);
        _client.Connect(_host, _port, writer);

        _logger.LogInformation("Connected on {Host}:{Port}", _host, _port);

        _playerIdAssignedEvent.Wait(Constants.ClientConnectionTimeoutMs);

        if (_netThreadContext.PlayerId == null)
            _logger.LogError("Failed to assign PlayerId within {Timeout} ms", Constants.ClientConnectionTimeoutMs);
    }
    
    public void RequestDisconnect()
    {
        if (!RequestedConnect)
        {
            _logger.LogWarning("Relay client is already disconnecting");
            return;
        }
        RequestedConnect = false;

        _logger.LogInformation("Explicitly disconnecting from {Host}:{Port}", _host, _port);

        OnRequestedDisconnect?.Invoke();
        
        _client.DisconnectAll();
        
        _playerIdAssignedEvent.Reset();
    }

    public void RequestReconnect()
    {
        RequestDisconnect();
        RequestConnect();
    }

    public void RequestJoinArea(AreaId areaId)
    {
        if (!RequestedConnect)
        {
            _logger.LogError("Relay client is not connected to the server");
            return;
        }

        if (RequestedAreaId != null)
        {
            _logger.LogWarning("Already requested to join a different area {AreaId}", RequestedAreaId.Value);
            RequestLeaveArea();
        }

        if (RequestedAreaId == areaId)
        {
            _logger.LogWarning("Already requested to join area {AreaId}", areaId);
            return;
        }
        RequestedAreaId = areaId;
        
        var playerId = PlayerId;
        if (playerId == null)
            throw new Exception("PlayerId cannot be null");
        
        var writer = new NetDataWriter();
        writer.Put((byte)RelayMessageCode.RequestAreaEvent);
        writer.Put(playerId.Value);
        writer.Put(true); // Request joining area event
        areaId.Serialize(writer);
        SendRawMessage(writer, DeliveryMethod.ReliableOrdered);
    }

    public void RequestLeaveArea()
    {
        if (!RequestedConnect)
        {
            _logger.LogError("Relay client is not connected to the server");
            return;
        }
        
        if (RequestedAreaId == null)
        {
            _logger.LogWarning("Already requested to leave area");
            return;
        }
        RequestedAreaId = null;
        
        var playerId = PlayerId;
        if (playerId == null)
            throw new Exception("PlayerId cannot be null");
        
        var writer = new NetDataWriter();
        writer.Put((byte)RelayMessageCode.RequestAreaEvent);
        writer.Put(playerId.Value);
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
        var playerId = PlayerId;
        if (playerId == null)
            throw new Exception("PlayerId cannot be null");

        var message = RelayMessage.ToPeers(eventCode, playerId.Value, peers, deliveryMethod);
        data.Serialize(message.Writer);
        SendMessage(message);
    }

    public void SendMessageRelayMode<T>(RelayMessageCode eventCode, T data, RelayMode mode, DeliveryMethod deliveryMethod)
        where T : INetSerializable
    {
        var playerId = PlayerId;
        if (playerId == null)
            throw new Exception("PlayerId cannot be null");

        var message = RelayMessage.ByRelayMode(eventCode, playerId.Value, mode, deliveryMethod);
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
                if (_netThreadContext.PlayerId != null && _netThreadContext.PlayerId != playerId)
                {
                    _logger.LogError("Missing handshake for player {PlayerId} but already assigned {AssignedPlayerId}", playerId, _netThreadContext.PlayerId);
                }
                _netThreadContext.IsConnected = true;
                _netThreadContext.PlayerId = playerId;
                _netThreadContext.AllPlayers.Add(playerId);
                
                _playerIdAssignedEvent.Set();
                
                var otherPlayerCount = reader.GetInt();
                for (var i = 0; i < otherPlayerCount; i++)
                {
                    var otherPlayerId = reader.Get<PlayerId>();
                    if (!_netThreadContext.AllPlayers.Contains(otherPlayerId))
                    {
                        _netThreadContext.AllPlayers.Add(otherPlayerId);
                    }
                    else
                    {
                        _logger.LogError("Received handshake for player {PlayerId} that already is marked as connected", otherPlayerId);
                    }
                }
                
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
                    if (_netThreadContext.PlayerId == null)
                    {
                        _logger.LogError("Received handshake for joining area {AreaId} by player {PlayerId} but PlayerId is not set", playerId, _netThreadContext.PlayerId);
                        break;
                    }
                    if (playerId != PlayerId)
                    {
                        _logger.LogError("Received handshake for player {PlayerId} but expected {ExpectedPlayerId}", playerId, PlayerId);
                        break;
                    }

                    if (_netThreadContext.CurrentAreaId != null)
                    {
                        _logger.LogError("Received handshake for joining area {AreaId} by player {PlayerId} but already in area {CurrentArea}", playerId, _netThreadContext.PlayerId, _netThreadContext.CurrentAreaId);
                        break;
                    }
                    AreaId areaId = default;
                    areaId.Deserialize(reader);

                    _logger.LogInformation("NETWORK JOINING {AreaId} by player {PlayerId}", areaId, playerId);
                    
                    _netThreadContext.CurrentAreaId = areaId;
                    _netThreadContext.AreaPlayers.Clear();
                    _netThreadContext.AreaPlayers.Add(playerId);

                    var playerInAreaCount = reader.GetUShort();

                    for (var i = 0; i < playerInAreaCount; i++)
                    {
                        var otherPlayerId = reader.Get<PlayerId>();
                        _netThreadContext.AreaPlayers.Add(otherPlayerId);
                    }

                    OnJoinedArea?.Invoke(_netThreadContext, areaId);
                }
                else
                {
                    if (_netThreadContext.PlayerId == null)
                    {
                        _logger.LogError("Received handshake for leaving area by player {PlayerId} but PlayerId is not set", playerId);
                        break;
                    }
                    if (playerId != PlayerId)
                    {
                        _logger.LogError("Received handshake for player {PlayerId} but expected {ExpectedPlayerId}", playerId, PlayerId);
                        break;
                    }

                    if (_netThreadContext.CurrentAreaId == null)
                    {
                        _logger.LogError("Received handshake for leaving area by player {PlayerId} but not in any area", playerId);
                        break;
                    }
                    
                    OnLeftArea?.Invoke(_netThreadContext);
                    
                    _logger.LogInformation("NETWORK LEAVING {AreaId} by player {PlayerId}", _netThreadContext.CurrentAreaId, playerId);

                    _netThreadContext.CurrentAreaId = null;
                    _netThreadContext.AreaPlayers.Remove(playerId);
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
                        if (_netThreadContext.AreaPlayers.Contains(playerId))
                        {
                            _logger.LogInformation("Player disconnected event for player {PlayerId} that is still in the area", playerId);
                            _netThreadContext.AreaPlayers.Remove(playerId);
                        }
                        OnOtherPlayerDisconnected?.Invoke(_netThreadContext, playerId);
                        _netThreadContext.AllPlayers.Remove(playerId);
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
                    if (_netThreadContext.CurrentAreaId == null)
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
                        OnOtherPlayerLeftArea?.Invoke(_netThreadContext, playerId);
                        _netThreadContext.AreaPlayers.Remove(playerId);
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
                if (eventCode >= RelayMessageCode.MinBuiltInEvent)
                {
                    var serverHeader = new ServerEventHeader(eventCode, Api.Multiplayer.Idents.PlayerId.Server);
                    var serverHandler = _serverMessageHandlers[(byte)eventCode];

                    if (serverHandler != null)
                    {
                        var position = reader.Position;
                        foreach (var handlerUntyped in serverHandler.GetInvocationList())
                        {
                            reader.SetPosition(position);
                            var handler = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>)handlerUntyped;
                            handler.Invoke(_netThreadContext, serverHeader, reader);
                        }
                    }
                    return;
                }
                
                if (eventCode >= RelayMessageCode.MinServerRpcEvent)
                {
                    var serverHeader = new ServerEventHeader(eventCode, Api.Multiplayer.Idents.PlayerId.Server);
                    var serverHandler = _serverMessageHandlers[(byte)eventCode];
                    
                    if (serverHandler != null)
                    {
                        var position = reader.Position;
                        foreach (var handlerUntyped in serverHandler.GetInvocationList())
                        {
                            reader.SetPosition(position);
                            var handler = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>)handlerUntyped;
                            handler.Invoke(_netThreadContext, serverHeader, reader);
                        }
                    }
                    return;
                }

                {
                    var clientHeader = reader.GetCustomRelayEventHeader(eventCode);
                    var clientHandler = _clientMessageHandlers[(byte)eventCode];
                
                    if (clientHandler != null)
                    {
                        var position = reader.Position;
                        foreach (var handlerUntyped in clientHandler.GetInvocationList())
                        {
                            reader.SetPosition(position);
                            var handler = (Action<IRelayClientNetworkThreadContext, CustomRelayEventHeader, NetDataReader>)handlerUntyped;
                            handler.Invoke(_netThreadContext, clientHeader, reader);
                        }
                    }
                }
                
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
    
    // NOTE: This indicates getting disconnected from the server, not other peers getting disconnected
    private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo info)
    {
        _logger.LogInformation("Disconnected from server: {Reason}", info.Reason);
        
        _netThreadContext.CurrentAreaId = null;
        _netThreadContext.IsConnected = false;
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
