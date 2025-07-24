using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public sealed class RelayClient : IShimRecordableRelayClient
{
    private RelaySerializer _serializer { get; }

    private readonly RelayConnectionOptions _options;
    private readonly string _host;
    private readonly int _port;

    private readonly Random _rng = new(2137);

    private int _requestCounter;
    private int GetNextRequestId() => ++_requestCounter;
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _client;
    private readonly ILogger _logger;

    private Thread? _clientThread;
    private bool _isRunning;

    private readonly ConcurrentDictionary<int, TaskCompletionSource<BlobInfo?>> _blobDownloadTasks = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _blobUploadTasks = new();

    public Dictionary<object, object> RoomState { get; private set; } = new();
    public Player LocalPlayer { get; private set; } = new(new Dictionary<object, object>());
    public ConcurrentDictionary<PlayerId, Player> OtherPlayers { get; } = new();

    public bool IsRunning => _isRunning;
    public bool Connected { get; private set; }

    // FIXME: Move this to game-specific code
    public bool InRoom { get; private set; }

    public PlayerId PlayerId => LocalPlayer.PlayerId;

    // FIXME: Move this to game-specific code
    public bool IsMasterClient
    {
        get
        {
            if (!RoomState.TryGetValue(RoomProperties.MasterClientId, out var untypedMasterPlayerId))
                return false;
            var masterPlayerId = (PlayerId)untypedMasterPlayerId;
            return masterPlayerId != PlayerId.Invalid && LocalPlayer.PlayerId == masterPlayerId;
        }
    }

    public event Action? OnBeforeStart;
    public event Action? OnAfterStart;
    public event Action? OnBeforeStop;
    public event Action? OnAfterStop;

    public event Action<PlayerId, Dictionary<object, object?>>? OnPlayerPropertiesAdded;
    public event Action<Dictionary<object, object?>>? OnRoomPropertiesChanged;
    public event Action<PlayerId, Dictionary<object, object?>>? OnPlayerPropertiesChanged;

    public event Action<CustomEventHeader, NetDataReader>? OnCustomEvent
    {
        add => this[(byte)SystemEvent.MinCustomEvent, (byte)SystemEvent.MaxCustomEvent].OnCustomEvent += value;
        remove => this[(byte)SystemEvent.MinCustomEvent, (byte)SystemEvent.MaxCustomEvent].OnCustomEvent -= value;
    }

    /// <summary>
    /// Event that is raised when a custom event is received from the server.
    /// Raised on the thread that the LiteNetLib client is running on.
    /// </summary>
    public CustomEventEntry this[byte minEventCode, byte maxEventCode]
        => new(this, minEventCode, maxEventCode);

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

    public void AddServerRpcEventHandler(ServerRpcEventRange eventRange, Action<ServerRpcEventHeader, NetDataReader>? value)
    {
        for (var eventCode = eventRange.MinEventCode; eventCode <= eventRange.MaxEventCode; eventCode++)
        {
            AddServerRpcEventHandler(new ServerRpcEventEntry(eventCode), value);
        }
    }

    public void RemoveServerRpcEventHandler(ServerRpcEventEntry eventEntry, Action<ServerRpcEventHeader, NetDataReader>? value)
    {
        _serverRpcEventHandlers[eventEntry.EventCode] = (Action<ServerRpcEventHeader, NetDataReader>?)Delegate.Remove(_serverRpcEventHandlers[eventEntry.EventCode], value);
    }

    public void RemoveServerRpcEventHandler(ServerRpcEventRange eventRange, Action<ServerRpcEventHeader, NetDataReader>? value)
    {
        for (var eventCode = eventRange.MinEventCode; eventCode <= eventRange.MaxEventCode; eventCode++)
        {
            RemoveServerRpcEventHandler(new ServerRpcEventEntry(eventCode), value);
        }
    }

    public event Action<PlayerId, Dictionary<object, object>>? OnPeerIdAssigned;
    public event Action? OnBeforeJoinedRoom;
    public event Action<Dictionary<object, object>>? OnAfterJoinedRoom;
    public event Action<DisconnectReason>? OnDisconnected;
    public event Action<int>? OnPingUpdated;
    public event Action<NetDataReader>? OnEcsSnapshot;
    public event Action<NetDataReader>? OnEcsDelta;
    public event Action<NetworkIdComponent>? OnReceivedDeleteEntity;
    public event Action<int, bool>? OnBlobAck;
    public event Action<int, BlobInfo?>? OnBlobData;

    /// <summary>
    /// At this point the connecting player has been assigned an ID and we have synced their state.
    /// </summary>
    public event Action<PlayerId, Dictionary<object, object>>? OnOtherPlayerJoined;

    public event Action<PlayerId>? OnOtherPlayerLeft;

    public event Action? OnEnterRoomRequest;
    public event Action? OnExitRoomRequest;

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

    public RelayClient(string host, int port, RelayConnectionOptions options, RelaySerializer serializer, ILogger logger)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _options = options;
        _host = host;
        _port = port;

        _listener = new EventBasedNetListener();
        _listener.NetworkReceiveEvent += OnListenerOnNetworkReceiveEvent;
        _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;
        _listener.PeerDisconnectedEvent += OnServerDisconnected;

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
            UpdateTime = Constants.ClientTickRateMs,
        };
        _logger = logger;
    }

    public int GetMaxPacketSize(DeliveryMethod deliveryMethod)
    {
        return Server?.GetMaxSinglePacketSize(deliveryMethod) ?? 1300;
    }

    private void OnServerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        _logger.LogInformation("Disconnected from server: {Reason}", info.Reason);
        InRoom = false;
        Connected = false;
        OnDisconnected?.Invoke(info.Reason);
    }

    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogError("Relay client is already running");
            return;
        }

        _logger.LogDebug("Starting relay client on {Host}:{Port}", _host, _port);

        OnBeforeStart?.Invoke();

        _client.Start();

        var writer = new NetDataWriter();
        _options.Serialize(writer);

        _client.Connect(_host, _port, writer);

        _isRunning = true;
        _clientThread = new Thread(() =>
        {
            _logger.LogInformation("Running relay client on {Host}:{Port}", _host, _port);
            while (_isRunning)
            {
                try
                {
                    _client.PollEvents();
                    Thread.Sleep(Constants.ClientTickRateMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unhandled exception in client thread: {0} | {1}", ex.Message, ex.StackTrace);
                    var inner = ex.InnerException;
                    while (inner != null)
                    {
                        _logger.LogError("Inner exception: {0} | {1}", inner.Message, inner.StackTrace);
                        inner = inner.InnerException;
                    }
                }
            }
        });

        _clientThread.Start();

        OnAfterStart?.Invoke();

        _logger.LogDebug("Started relay client on {Host}:{Port}", _host, _port);
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            _logger.LogInformation("Relay client is not running");
            return;
        }

        _logger.LogDebug("Stopping relay client on {Host}:{Port}", _host, _port);

        OnBeforeStop?.Invoke();

        _isRunning = false;
        _client.Stop();
        _clientThread?.Join();
        _clientThread = null;
        LocalPlayer = new Player(new Dictionary<object, object>());
        RoomState.Clear();
        OtherPlayers.Clear();

        InRoom = false;
        if (Connected)
        {
            Connected = false;
            OnDisconnected?.Invoke(DisconnectReason.DisconnectPeerCalled);
        }

        OnAfterStop?.Invoke();

        _logger.LogDebug("Stopped relay client on {Host}:{Port}", _host, _port);
    }

    public Player? GetPlayerState(PlayerId playerId)
    {
        if (playerId == LocalPlayer.PlayerId)
        {
            return LocalPlayer;
        }
        else
        {
            OtherPlayers.TryGetValue(playerId, out var otherPlayer);
            return otherPlayer;
        }
    }

    public void SendMessageToServer(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        Server?.Send(writer, deliveryMethod);
        var ev = writer.Data[0];
        AppendToSentStats(ev, writer.Length);
    }

    public void OpSetCustomPropertiesOfActor(PlayerId playerId, Dictionary<object, object?> data)
    {
        if (!Connected && !InRoom)
        {
            _logger.LogWarning("Attempted to set properties of player {0} while not in room", playerId);
            return;
        }

        if (Connected && !InRoom)
        {
            RelaySerializer.UpdateAndGetDiff(LocalPlayer.Properties, data);
            return;
        }

        // connected and in room, send the update to the server
        var writer = CreatePlayerPropertiesUpdatePacket(playerId, data);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);
    }

    public void OpSetCustomPropertiesOfRoom(Dictionary<object, object?> data)
    {
        var diff = RelaySerializer.UpdateAndGetDiff(RoomState, data);
        var writer = CreateRoomPropertiesUpdatePacket(diff);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Send an event to a specific player or group of players.
    /// This overload does not support event caching, as cached events must either be sent to all other players or all players.
    /// </summary>
    public void OpRaiseEvent(byte eventCode, object? data, PlayerId[] peers, DeliveryMethod deliveryMethod)
    {
        var writer = new NetDataWriter();
        writer.PutCustomEventHeader(eventCode, LocalPlayer.PlayerId, peers, EventCaching.DoNotCache);

        if (data != null)
        {
            _serializer.SerializeObject(writer, data);
        }

        SendMessageToServer(writer, deliveryMethod);
    }

    /// <summary>
    /// Send an event with a specific delivery method. This overload does not support event caching.
    /// </summary>
    public void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod)
    {
        var writer = new NetDataWriter();
        writer.PutCustomEventHeader(eventCode, LocalPlayer.PlayerId, mode, EventCaching.DoNotCache);

        if (data != null)
        {
            _serializer.SerializeObject(writer, data);
        }

        SendMessageToServer(writer, deliveryMethod);
    }

    public void OpRaiseEventRaw(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        SendMessageToServer(writer, deliveryMethod);
    }

    /// <summary>
    /// Send an event that will be cached by the server and sent to all/other players (depending on the eventCaching parameter).
    /// </summary>
    public void OpRaiseEvent(byte eventCode, object? data, EventCaching eventCaching)
    {
        var writer = new NetDataWriter();

        // AddToRoomCacheGlobal events are sent to all players, AddToRoomCache - to others, DoNotCache - too, by default
        var mode = eventCaching == EventCaching.AddToRoomCacheGlobal ? RelayMode.All : RelayMode.Others;
        writer.PutCustomEventHeader(eventCode, LocalPlayer.PlayerId, mode, eventCaching);

        if (data != null)
        {
            _serializer.SerializeObject(writer, data);
        }

        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);
    }

    private readonly ConcurrentDictionary<byte, (long Count, long Bytes)> _statsSent = new();
    private readonly ConcurrentDictionary<byte, (long Count, long Bytes)> _statsRecv = new();

    private void AppendToSentStats(byte ev, long bytesSent)
    {
        _statsSent.AddOrUpdate(ev, (1, bytesSent), (_, data) => (data.Count + 1, data.Bytes + bytesSent));
    }

    private void AppendToRecvStats(byte ev, long bytesRecv)
    {
        _statsRecv.AddOrUpdate(ev, (1, bytesRecv), (_, data) => (data.Count + 1, data.Bytes + bytesRecv));
    }

    private void LogEventStats()
    {
        foreach (var kvp in _statsSent.OrderByDescending(x => x.Value))
        {
            _logger.LogTrace("Event {Event}: sent {Bytes} B, avg {Average} B", kvp.Key, kvp.Value.Bytes, kvp.Value.Bytes / kvp.Value.Count);
        }

        _logger.LogTrace("----------------------------------------");
        foreach (var kvp in _statsRecv.OrderByDescending(x => x.Value))
        {
            _logger.LogTrace("Event {Event}: recv {Bytes} B, avg {Average} B", kvp.Key, kvp.Value.Bytes, kvp.Value.Bytes / kvp.Value.Count);
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
    {
        var eventCode = reader.GetByte();
        AppendToRecvStats(eventCode, reader.UserDataSize);

        switch ((SystemEvent)eventCode)
        {
            case SystemEvent.HandshakePeerIdAssigned:
            {
                var playerId = reader.Get<PlayerId>();
                LocalPlayer.PlayerId = playerId;
                _logger.LogInformation("Assigned Actor ID {0}", LocalPlayer.PlayerId);

                var roomState = _serializer.DeserializeObject<Dictionary<object, object>>(reader);
                RoomState = roomState;
                Connected = true;
                return;
            }
            case SystemEvent.PlayerStateChanged:
            {
                var playerId = reader.Get<PlayerId>();
                var changes = _serializer.DeserializeObject<Dictionary<object, object?>>(reader);

                if (playerId == LocalPlayer.PlayerId)
                {
                    var diff = RelaySerializer.UpdateAndGetDiff(LocalPlayer.Properties, changes);
                    OnPlayerPropertiesChanged?.Invoke(playerId, diff);
                }
                else
                {
                    if (!OtherPlayers.TryGetValue(playerId, out var player))
                    {
                        _logger.LogDebug("Received initial state for player {0}", playerId);
                        OtherPlayers[playerId] = new Player(changes
                            .Where(x => x.Value != null)
                            .ToDictionary(x => x.Key, x => x.Value!));
                        OnPlayerPropertiesAdded?.Invoke(playerId, changes);
                    }
                    else
                    {
                        var diff = RelaySerializer.UpdateAndGetDiff(player.Properties, changes);
                        OnPlayerPropertiesChanged?.Invoke(playerId, diff);
                    }
                }

                return;
            }
            case SystemEvent.RoomStateChanged:
            {
                var changes = _serializer.DeserializeObject<Dictionary<object, object?>>(reader);
                var diff = RelaySerializer.UpdateAndGetDiff(RoomState, changes);
                OnRoomPropertiesChanged?.Invoke(diff);
                return;
            }
            case SystemEvent.PlayerJoined:
            {
                var playerId = reader.Get<PlayerId>();
                var initialState = _serializer.DeserializeObject<Dictionary<object, object>>(reader);
                var newPlayer = new Player(initialState);

                if (playerId == LocalPlayer.PlayerId)
                {
                    LocalPlayer = newPlayer;
                    OnBeforeJoinedRoom?.Invoke();
                    InRoom = true;
                    OnAfterJoinedRoom?.Invoke(initialState);
                }
                else
                {
                    if (!OtherPlayers.TryAdd(playerId, newPlayer))
                    {
                        _logger.LogInformation("Received PlayerJoined event for player {0} that already exists, perhaps they reconnected", playerId);
                        OtherPlayers[playerId] = newPlayer;
                    }

                    OnOtherPlayerJoined?.Invoke(playerId, initialState);
                }

                return;
            }
            case SystemEvent.PlayerLeft:
            {
                var playerId = reader.Get<PlayerId>();
                OnOtherPlayerLeft?.Invoke(playerId);
                return;
            }
            case SystemEvent.HandshakeSetInitialProperties:
                _logger.LogError("Event {Event} received, but should not be sent to the client", SystemEvent.HandshakeSetInitialProperties);
                return;
            case SystemEvent.EcsSnapshot:
                OnEcsSnapshot?.Invoke(reader);
                return;
            case SystemEvent.EcsUpdate:
                OnEcsDelta?.Invoke(reader);
                return;
            case SystemEvent.DestroyEntity:
            {
                var netId = reader.Get<NetworkIdComponent>();
                OnReceivedDeleteEntity?.Invoke(netId);
                return;
            }
            case SystemEvent.DownloadBlob:
            case SystemEvent.UploadBlob:
                _logger.LogError("Event {Event} received, but should not be sent to the client", SystemEvent.DownloadBlob);
                return;
            case SystemEvent.UploadBlobAck:
            {
                var requestId = reader.GetInt();
                var success = reader.GetBool();

                _logger.LogInformation("File upload with request ID {RequestId} completed with success: {Success}", requestId, success);

                if (!_blobUploadTasks.TryRemove(requestId, out var uploadTask))
                {
                    _logger.LogWarning("No task found for request ID {RequestId} when receiving upload ack", requestId);
                    return;
                }

                if (uploadTask.Task.IsCanceled)
                {
                    _logger.LogWarning("Upload task already cancelled, not setting result for request ID {RequestId}", requestId);
                    return;
                }

                if (uploadTask.TrySetResult(success))
                {
                    OnBlobAck?.Invoke(requestId, success);
                }
                else
                {
                    _logger.LogError("Failed to set result for file upload task with request ID {RequestId}", requestId);
                }

                return;
            }
            case SystemEvent.BlobData:
            {
                var requestId = reader.GetInt();
                var succeeded = reader.GetBool();

                _logger.LogInformation("File download with request ID {RequestId} completed with success: {Succeeded}", requestId, succeeded);

                if (!_blobDownloadTasks.TryRemove(requestId, out var downloadTask))
                {
                    _logger.LogError("No task found for request ID {RequestId}", requestId);
                    return;
                }

                BlobInfo? result = null;

                if (succeeded)
                {
                    var fileName = reader.GetString();
                    var fileSize = reader.GetInt();

                    var fileData = new byte[fileSize];
                    reader.GetBytes(fileData, fileSize);

                    _logger.LogInformation("Received file stream for {FileName} with request ID {RequestId}", fileName, requestId);
                    result = new BlobInfo(fileName, fileData);
                }
                else
                {
                    _logger.LogWarning("File download with request ID {RequestId} failed", requestId);
                }

                if (downloadTask.Task.IsCanceled)
                {
                    _logger.LogWarning("Download task already cancelled, not setting result for request ID {RequestId}", requestId);
                    return;
                }

                if (downloadTask.TrySetResult(result))
                {
                    OnBlobData?.Invoke(requestId, result);
                }
                else
                {
                    _logger.LogError("Failed to set result for file download task with request ID {RequestId}", requestId);
                }

                return;
            }
        }

        // it is a system rpc event
        if (eventCode >= (byte)SystemEvent.MinServerRpcEvent)
        {
            var serverRpcHeader = new ServerRpcEventHeader(eventCode, PlayerId.Server);
            var serverRpcEventHandler = _serverRpcEventHandlers[eventCode];
            serverRpcEventHandler?.Invoke(serverRpcHeader, reader);
            return;
        }

        var header = reader.GetCustomEventHeader(eventCode);
        var eventHandler = _customEventHandlers[eventCode];
        eventHandler?.Invoke(header, reader);
    }

    public void SendInitialPlayerState()
    {
        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.HandshakeSetInitialProperties);
        _serializer.SerializeObject(writer, LocalPlayer.Properties);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);
    }

    public async Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException();

        ct.ThrowIfCancellationRequested();

        var taskSource = new TaskCompletionSource<BlobInfo?>();

        var requestId = GetNextRequestId();
        _blobDownloadTasks[requestId] = taskSource;

        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.DownloadBlob);
        writer.Put(requestId);
        writer.Put(name);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);
        _logger.LogInformation("Requesting file download: {FileName} with request ID {RequestId}", name, requestId);

        using (ct.Register(() => taskSource.TrySetCanceled(), useSynchronizationContext: false))
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                // FIXME: Are we sure about ConfigureAwait(false) here?
                // This makes it possible to receive the answer on a different thread than the one that sent the request.
                return await taskSource.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("File download for {FileName} was cancelled with request ID {RequestId}", name, requestId);
                throw;
            }
            finally
            {
                _blobDownloadTasks.TryRemove(requestId, out _);
            }
        }
    }

    public async Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException();

        ct.ThrowIfCancellationRequested();

        var tcs = new TaskCompletionSource<bool>();

        var requestId = GetNextRequestId();
        _blobUploadTasks[requestId] = tcs;

        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.UploadBlob);
        writer.Put(requestId);
        writer.Put(blob.Name);
        writer.Put(blob.Content.Length);
        writer.Put(blob.Content);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);

        _logger.LogInformation("Uploading file: {FileName} with request ID {RequestId}", blob.Name, requestId);
        using (ct.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false))
        {
            try
            {
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("File upload for {FileName} was cancelled with request ID {RequestId}", blob.Name, requestId);
                throw;
            }
            finally
            {
                _blobUploadTasks.TryRemove(requestId, out _);
            }
        }
    }

    private long _lastBytesReceived;
    private long _lastBytesSent;
    private DateTimeOffset _lastStatCheck = DateTimeOffset.Now;

    private void OnNetworkLatencyUpdateEvent(NetPeer peer, int latency)
    {
        // Round trip time. LiteNetLib reports one way latency, so we double it.
        // We add a random jitter so that the results are not always divisible by 2.
        OnPingUpdated?.Invoke(2 * latency + _rng.Next(2));

        // Print stats every time too
        var dRecv = _client.Statistics.BytesReceived - _lastBytesReceived;
        _lastBytesReceived = _client.Statistics.BytesReceived;

        var dSent = _client.Statistics.BytesSent - _lastBytesSent;
        _lastBytesSent = _client.Statistics.BytesSent;

        var now = DateTimeOffset.Now;
        var delta = now - _lastStatCheck;

        _lastStatCheck = now;

        // print avg recv and sent over the delta time
        var avgRecv = (long)(dRecv / delta.TotalSeconds);
        var avgSent = (long)(dSent / delta.TotalSeconds);

#if LOG_NETWORKING_EVENTS
        _logger.LogDebug("Avg recv: {Recv} B/s, Avg sent: {Sent} B/s", avgRecv, avgSent);
        LogEventStats();
#endif
    }

    private NetDataWriter CreatePlayerPropertiesUpdatePacket(PlayerId playerId, Dictionary<object, object?> changes)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.PlayerStateChanged);
        writer.Put(playerId);
        _serializer.SerializeObject(writer, changes);
        return writer;
    }

    private NetDataWriter CreateRoomPropertiesUpdatePacket(Dictionary<object, object?> changes)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.RoomStateChanged);
        _serializer.SerializeObject(writer, changes);
        return writer;
    }

    // FIXME: Move this to game-specific code
    public void EnterRoom()
    {
        if (!Connected)
            throw new InvalidOperationException("Cannot enter room when not connected");

        _logger.LogDebug("Entering room requested");
        OnEnterRoomRequest?.Invoke();
        SendInitialPlayerState();
    }

    // FIXME: Move this to game-specific code
    public void ExitRoom()
    {
        if (!Connected)
            throw new InvalidOperationException("Cannot exit room when not connected");

        _logger.LogDebug("Exiting room requested");
        OnExitRoomRequest?.Invoke();
    }
}