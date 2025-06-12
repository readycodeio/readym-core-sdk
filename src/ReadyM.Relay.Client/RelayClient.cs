using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public sealed class RelayClient : RelayPeerBase, IBlobClient, IDisposable
{
    private readonly Guid _userGuid;
    private readonly string _host;
    private readonly int _port;

    private readonly Random _rng = new(2137);

    private int _requestCounter;
    private int GetNextRequestId() => ++_requestCounter;
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _client;
    private readonly Action<LogLevel, string, object?[]> _logger;

    private Thread? _clientThread;
    private bool _isRunning;

    private readonly ConcurrentDictionary<int, TaskCompletionSource<BlobInfo?>> _blobDownloadTasks = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _blobUploadTasks = new();

    public Dictionary<object, object> RoomState { get; private set; } = new();
    public Player LocalPlayer { get; private set; } = new(new Dictionary<object, object>());
    public ConcurrentDictionary<PlayerId, Player> OtherPlayers { get; } = new();

    public bool InRoom { get; private set; }
    public PlayerId PlayerId => LocalPlayer.PlayerId;

    public event Action<Dictionary<object, object?>>? OnRoomPropertiesChanged;
    public event Action<PlayerId, Dictionary<object, object?>>? OnPlayerPropertiesChanged;

    /// <summary>
    /// Event that is raised when a custom event is received from the server.
    /// Raised on the thread that the LiteNetLib client is running on.
    /// </summary>
    public event Action<CustomEventHeader, NetPacketReader>? OnCustomEvent;

    public event Action? OnBeforeJoinedRoom;
    public event Action? OnAfterJoinedRoom;
    public event Action<DisconnectReason>? OnDisconnected;
    public event Action<int>? OnPingUpdated;
    public event Action<NetPacketReader>? OnEcsDelta;
    public event Action<NetworkIdComponent>? OnReceivedDeleteEntity;

    /// <summary>
    /// At this point the connecting player has been assigned an ID and we have synced their state.
    /// </summary>
    public event Action<PlayerId>? OnOtherPlayerJoined;

    public event Action<PlayerId>? OnOtherPlayerLeft;

    private NetPeer? Server
    {
        get
        {
            if (_client.FirstPeer == null)
            {
                Log(LogLevel.Error, "Disconnected from server");
            }

            return _client.FirstPeer;
        }
    }

    public RelayClient(Guid userGuid, string host, int port, Action<LogLevel, string, object?[]> logger)
    {
        _userGuid = userGuid;
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
            UpdateTime = Constants.ClientTickRateMs
        };
        _logger = logger;
    }

    public int GetMaxPacketSize(DeliveryMethod deliveryMethod)
    {
        return Server?.GetMaxSinglePacketSize(deliveryMethod) ?? 1300;
    }

    private void OnServerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        InRoom = false;
        OnDisconnected?.Invoke(info.Reason);
    }

    public void Start()
    {
        if (_isRunning)
        {
            Log(LogLevel.Error, "Relay client is already running");
            return;
        }

        _client.Start();
        _client.Connect(_host, _port, _userGuid.ToString());

        _isRunning = true;
        _clientThread = new Thread(() =>
        {
            Log(LogLevel.Information, "Running relay client on port {0}", _port);
            while (_isRunning)
            {
                try
                {
                    _client.PollEvents();
                    Thread.Sleep(Constants.ClientTickRateMs);
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, "Unhandled exception in client thread: {0} | {1}", ex.Message, ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        Log(LogLevel.Error, "Inner exception: {0} | {1}", ex.InnerException.Message, ex.InnerException.StackTrace);
                    }
                }
            }
        });

        _clientThread.Start();
    }

    public void Stop()
    {
        _client.Stop();
        _isRunning = false;
        _clientThread?.Join();
        _clientThread = null;
        InRoom = false;
        LocalPlayer = new Player(new Dictionary<object, object>());
        RoomState.Clear();
        OtherPlayers.Clear();
        OnDisconnected?.Invoke(DisconnectReason.DisconnectPeerCalled);
    }

    public Player? GetPlayerState(PlayerId playerId)
    {
        return playerId == LocalPlayer.PlayerId ? LocalPlayer : OtherPlayers.GetValueOrDefault(playerId);
    }

    public void SendMessageToServer(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        Server?.Send(writer, deliveryMethod);
        var ev = writer.Data[0];
        AppendToSentStats(ev, writer.Length);
    }

    public void OpSetCustomPropertiesOfActor(PlayerId playerId, Dictionary<object, object?> data)
    {
        if (!InRoom)
        {
            if (playerId == Constants.UnsetPeerId)
            {
                UpdateAndGetDiff(LocalPlayer.Properties, data);
            }
            else
            {
                Log(LogLevel.Warning, "Attempted to set properties of player {0} while not in room", playerId);
            }

            return;
        }

        var writer = CreatePlayerPropertiesUpdatePacket(playerId, data);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);
    }

    public void OpSetCustomPropertiesOfRoom(Dictionary<object, object?> data)
    {
        var diff = UpdateAndGetDiff(RoomState, data);
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
            SerializeObject(writer, data);
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
            SerializeObject(writer, data);
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
            SerializeObject(writer, data);
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
#if DEBUG
        foreach (var kvp in _statsSent.OrderByDescending(x => x.Value))
        {
            Log(LogLevel.Debug, "Event {Event}: sent {Bytes} B, avg {Average} B", kvp.Key, kvp.Value.Bytes, kvp.Value.Bytes / kvp.Value.Count);
        }

        Log(LogLevel.Debug, "----------------------------------------");
        foreach (var kvp in _statsRecv.OrderByDescending(x => x.Value))
        {
            Log(LogLevel.Debug, "Event {Event}: recv {Bytes} B, avg {Average} B", kvp.Key, kvp.Value.Bytes, kvp.Value.Bytes / kvp.Value.Count);
        }
#endif
    }

    public void Dispose()
    {
        Stop();
    }

    private void Log(LogLevel level, [StructuredMessageTemplate] string template, params object?[] values)
    {
        _logger(level, template, values);
    }

    private void OnListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliverymethod)
    {
        var eventCode = reader.GetByte();
        AppendToRecvStats(eventCode, reader.UserDataSize);

        switch ((SystemEvent)eventCode)
        {
            case SystemEvent.HandshakePeerIdAssigned:
            {
                LocalPlayer.PlayerId = reader.Get<PlayerId>();
                Log(LogLevel.Information, "Assigned Actor ID {0}", LocalPlayer.PlayerId);

                var roomState = DeserializeObject<Dictionary<object, object>>(reader);
                RoomState = roomState;

                // send joined room event
                var writer = new NetDataWriter();
                writer.Put((byte)SystemEvent.HandshakeSetInitialProperties);
                SerializeObject(writer, LocalPlayer.Properties);
                SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);

                return;
            }
            case SystemEvent.PlayerStateChanged:
            {
                var playerId = reader.Get<PlayerId>();
                var changes = DeserializeObject<Dictionary<object, object?>>(reader);

                if (playerId == LocalPlayer.PlayerId)
                {
                    var diff = UpdateAndGetDiff(LocalPlayer.Properties, changes);
                    OnPlayerPropertiesChanged?.Invoke(playerId, diff);
                }
                else
                {
                    if (!OtherPlayers.TryGetValue(playerId, out var player))
                    {
                        Log(LogLevel.Debug, "Received initial state for player {0}", playerId);
                        OtherPlayers[playerId] = new Player(changes
                            .Where(x => x.Value != null)
                            .ToDictionary(x => x.Key, x => x.Value!));
                    }
                    else
                    {
                        var diff = UpdateAndGetDiff(player.Properties, changes);
                        OnPlayerPropertiesChanged?.Invoke(playerId, diff);
                    }
                }

                return;
            }
            case SystemEvent.RoomStateChanged:
            {
                var changes = DeserializeObject<Dictionary<object, object?>>(reader);
                var diff = UpdateAndGetDiff(RoomState, changes);
                OnRoomPropertiesChanged?.Invoke(diff);
                return;
            }
            case SystemEvent.PlayerJoined:
            {
                var playerId = reader.Get<PlayerId>();
                var initialState = DeserializeObject<Dictionary<object, object>>(reader);
                var newPlayer = new Player(initialState);

                if (playerId == LocalPlayer.PlayerId)
                {
                    LocalPlayer = newPlayer;
                    OnBeforeJoinedRoom?.Invoke();
                    InRoom = true;
                    OnAfterJoinedRoom?.Invoke();
                }
                else
                {
                    if (!OtherPlayers.TryAdd(playerId, newPlayer))
                    {
                        Log(LogLevel.Information, "Received PlayerJoined event for player {0} that already exists, perhaps they reconnected", playerId);
                        OtherPlayers[playerId] = newPlayer;
                    }

                    OnOtherPlayerJoined?.Invoke(playerId);
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
                Log(LogLevel.Error, "Event {Event} received, but should not be sent to the client", SystemEvent.HandshakeSetInitialProperties);
                return;
            case SystemEvent.EcsUpdate:
                OnEcsDelta?.Invoke(reader);
                return;
            case SystemEvent.DestroyEntity:
                var netId = reader.GetNetworkId();
                OnReceivedDeleteEntity?.Invoke(netId);
                return;
            case SystemEvent.DownloadBlob:
            case SystemEvent.UploadBlob:
                Log(LogLevel.Error, "Event {Event} received, but should not be sent to the client", SystemEvent.DownloadBlob);
                return;
            case SystemEvent.UploadBlobAck:
            {
                var requestId = reader.GetInt();
                var success = reader.GetBool();

                Log(LogLevel.Information, "File upload with request ID {RequestId} completed with success: {Success}", requestId, success);

                if (!_blobUploadTasks.TryRemove(requestId, out var uploadTask))
                {
                    Log(LogLevel.Warning, "No task found for request ID {RequestId} when receiving upload ack", requestId);
                    return;
                }

                if (uploadTask.Task.IsCanceled)
                {
                    Log(LogLevel.Warning, "Upload task already cancelled, not setting result for request ID {RequestId}", requestId);
                    return;
                }

                if (!uploadTask.TrySetResult(success))
                {
                    Log(LogLevel.Error, "Failed to set result for file upload task with request ID {RequestId}", requestId);
                }

                return;
            }
            case SystemEvent.BlobData:
            {
                var requestId = reader.GetInt();
                var succeeded = reader.GetBool();

                if (!_blobDownloadTasks.TryRemove(requestId, out var downloadTask))
                {
                    Log(LogLevel.Error, "No task found for request ID {RequestId}", requestId);
                    return;
                }

                BlobInfo? result = null;

                if (succeeded)
                {
                    var fileName = reader.GetString();
                    var fileData = reader.GetBytesWithLength();

                    Log(LogLevel.Information, "Received file stream for {FileName} with request ID {RequestId}", fileName, requestId);
                    result = new BlobInfo(fileName, fileData);
                }
                else
                {
                    Log(LogLevel.Warning, "File download with request ID {RequestId} failed", requestId);
                }

                if (downloadTask.Task.IsCanceled)
                {
                    Log(LogLevel.Warning, "Download task already cancelled, not setting result for request ID {RequestId}", requestId);
                    return;
                }

                if (!downloadTask.TrySetResult(result))
                {
                    Log(LogLevel.Error, "Failed to set result for file download task with request ID {RequestId}", requestId);
                }

                return;
            }
        }

        var header = reader.GetCustomEventHeader(eventCode);
        OnCustomEvent?.Invoke(header, reader);
    }

    public async Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var taskSource = new TaskCompletionSource<BlobInfo?>();

        var requestId = GetNextRequestId();
        _blobDownloadTasks[requestId] = taskSource;

        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.DownloadBlob);
        writer.Put(requestId);
        writer.Put(name);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);
        Log(LogLevel.Information, "Requesting file download: {FileName} with request ID {RequestId}", name, requestId);

        await using (ct.Register(() => taskSource.TrySetCanceled(), useSynchronizationContext: false))
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                return await taskSource.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Log(LogLevel.Warning, "File download for {FileName} was cancelled with request ID {RequestId}", name, requestId);
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
        ct.ThrowIfCancellationRequested();

        var tcs = new TaskCompletionSource<bool>();

        var requestId = GetNextRequestId();
        _blobUploadTasks[requestId] = tcs;

        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.UploadBlob);
        writer.Put(requestId);
        writer.Put(blob.Name);
        writer.PutBytesWithLength(blob.Content);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);

        Log(LogLevel.Information, "Uploading file: {FileName} with request ID {RequestId}", blob.Name, requestId);
        await using (ct.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false))
        {
            try
            {
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                Log(LogLevel.Warning, "File upload for {FileName} was cancelled with request ID {RequestId}", blob.Name, requestId);
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

        Log(LogLevel.Debug, "Avg recv: {Recv} B/s, Avg sent: {Sent} B/s", avgRecv, avgSent);
        LogEventStats();
    }

    private NetDataWriter CreatePlayerPropertiesUpdatePacket(PlayerId playerId, Dictionary<object, object?> changes)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.PlayerStateChanged);
        writer.Put(playerId);
        SerializeObject(writer, changes);
        return writer;
    }

    private NetDataWriter CreateRoomPropertiesUpdatePacket(Dictionary<object, object?> changes)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.RoomStateChanged);
        SerializeObject(writer, changes);
        return writer;
    }
}