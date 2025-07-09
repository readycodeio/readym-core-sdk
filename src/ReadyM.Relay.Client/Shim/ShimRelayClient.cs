using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Shim;

namespace ReadyM.Relay.Client.Shim;

public class ShimRelayClient : IRelayClient
{
    private const int MaxTimeout = 120_000;
    
    private int _requestCounter;
    private int GetNextRequestId() => ++_requestCounter;
    
    public Dictionary<object, object> RoomState { get; private set; } = new();
    public Player LocalPlayer { get; private set; } = new(new Dictionary<object, object>());
    public ConcurrentDictionary<PlayerId, Player> OtherPlayers { get; } = new();

    public bool IsRunning => _isRunning;
    public bool Connected { get; private set; }
    public bool InRoom { get; private set; }
    public PlayerId PlayerId => LocalPlayer.PlayerId;

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

    private readonly List<ShimItem> _pendingItems = new List<ShimItem>();

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
    
    public event Action? OnEnterRoomRequest;
    public event Action? OnExitRoomRequest;

    public bool IsPlaying
        => _isPlaying;
    
    private ShimRecording? _recording;
    private bool _isPlaying;
    private int _delay;

    private readonly object _lock = new();
    private bool _isRunning;
    private bool _requestedEnterRoom;
    private Thread? _clientThread;
    
    private readonly ConcurrentDictionary<int, TaskCompletionSource<BlobInfo>> _blobDownloadTasks = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _blobUploadTasks = new();
    private readonly ILogger _logger;

    public ShimRelayClient(ILogger logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        Stop();
    }
    
    public void SetRecording(ShimRecording recording, int delay = 1_000)
    {
        if (_isRunning)
            throw new InvalidOperationException("Cannot set recording while the client is running");
        _delay = delay;
        _recording = recording;
    }
    
    public int GetMaxPacketSize(DeliveryMethod deliveryMethod)
        => 1300;

    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogError("Shim relay client is already running");
            return;
        }
        
        OnBeforeStart?.Invoke();
        
        _isRunning = true;
        
        OnAfterStart?.Invoke();
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            _logger.LogInformation("Shim relay client is not running");
            return;
        }
        
        if (_isPlaying)
        {
            StopPlaying();
        }

        OnBeforeStop?.Invoke();
        
        _requestedEnterRoom = false;
        _isRunning = false;
        Connected = false;
        
        LocalPlayer = new Player(new Dictionary<object, object>());
        OtherPlayers.Clear();
        InRoom = false;
        if (Connected)
        {
            Connected = false;
            OnDisconnected?.Invoke(DisconnectReason.DisconnectPeerCalled);
        }
        
        OnAfterStop?.Invoke();
    }
    
    public void StartPlaying()
    {
        if (!_isRunning)
            throw new InvalidOperationException("Shim relay client is not running");
        
        if (_isPlaying)
        {
            _logger.LogError("Shim relay client is already playing");
            return;
        }

        _isPlaying = true;
        
        _clientThread = new Thread(() =>
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var index = 0;
            _logger.LogInformation("Running shim relay client");
            while (true)
            {
                lock (_lock)
                {
                    if (!_isRunning)
                        break;
                }
                
                if (_recording == null)
                    return;

                Thread.Sleep(Constants.ShimClientTickRateMs);
                
                var elapsed = stopwatch.ElapsedMilliseconds - _delay;

                ProcessPendingItems();
                
                ShimItem item;
                lock (_recording)
                {

                    if (index >= _recording.Items.Count)
                        continue;

                    item = _recording.Items[index];
                    if (item.Elapsed > elapsed)
                        continue;
                }
                
                try
                {
                    if (!ProcessItem(item))
                    {
                        lock (_pendingItems)
                        {
                            _pendingItems.Add(item);
                        }
                    }
                    index++;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unhandled exception in shim client thread: {0} | {1}", ex.Message, ex.StackTrace);
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
    }

    private void ProcessPendingItems()
    {
        lock (_pendingItems)
        {
            var newPendingItems = new List<ShimItem>();
                    
            for (var i = 0; i < _pendingItems.Count; i++)
            {
                var pendingItem = _pendingItems[i];
                if (!ProcessItem(pendingItem))
                    newPendingItems.Add(pendingItem);
            }
                    
            _pendingItems.Clear();
            _pendingItems.AddRange(newPendingItems);
        }
    }

    public void StopPlaying()
    {
        if (!_isPlaying)
        {
            _logger.LogError("Shim relay client is not playing");
            return;
        }
        
        _isPlaying = false;
        
        _clientThread?.Join();
        _clientThread = null;
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

    private string GetPropertiesString(Dictionary<object, object?> data)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        var first = true;
        foreach (var kvp in data)
        {
            if (!first)
                sb.Append(",");
            first = false;
            sb.AppendFormat("{0}: {1}, ", kvp.Key, kvp.Value);
        }
        sb.Append("}");
        return sb.ToString();
    }

    public void SendMessageToServer(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        _logger.LogTrace("Sending message to server with delivery method {DeliveryMethod}", deliveryMethod);
    }

    public void OpSetCustomPropertiesOfActor(PlayerId playerId, Dictionary<object, object?> data)
    {
        _logger.LogTrace("Setting custom properties for player {PlayerId}: {Data}", playerId, GetPropertiesString(data));
    }

    public void OpSetCustomPropertiesOfRoom(Dictionary<object, object?> data)
    {
        _logger.LogTrace("Setting custom properties for room {Data}", GetPropertiesString(data));
    }

    public void OpRaiseEvent(byte eventCode, object? data, PlayerId[] peers, DeliveryMethod deliveryMethod)
    {
        _logger.LogTrace("Raise event code: {EventCode}", eventCode);
    }

    public void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod)
    {
        _logger.LogTrace("Raise event code: {EventCode}", eventCode);
    }

    public void OpRaiseEvent(byte eventCode, object? data, EventCaching eventCaching)
    {
        _logger.LogTrace("Raise event code: {EventCode}", eventCode);
    }

    public void OpRaiseEventRaw(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        _logger.LogTrace("Raise event code: {DeliveryMethod}", deliveryMethod);
    }

    public void SendInitialPlayerState()
    {
        if (!Connected)
            throw new InvalidOperationException("Cannot enter room when not connected");
        
        _logger.LogDebug("Sending initial player state for player {PlayerId}", LocalPlayer.PlayerId);
        
        lock (_lock)
        {
            _requestedEnterRoom = true;
        }

        ProcessPendingItems();
    }

    public bool ProcessItem(ShimItem item)
    {
        switch (item.Kind)
        {
            case ShimItemKind.PeerIdAssigned:
            {
                var playerId = item.PlayerId;
                var roomState = item.InitialState;
                
                LocalPlayer.PlayerId = playerId;
                RoomState = roomState.Collection;
                Connected = true;
                
                OnPeerIdAssigned?.Invoke(playerId, roomState.Collection);
                break;
            }
            case ShimItemKind.PlayerPropertiesAdded:
            case ShimItemKind.PlayerPropertiesChanged:
            {
                if (!InRoom)
                    return false;

                var playerId = item.PlayerId;
                var changes = item.Changes;

                if (playerId == LocalPlayer.PlayerId)
                {
                    var diff = RelaySerializer.UpdateAndGetDiff(LocalPlayer.Properties, changes.Collection);
                    OnPlayerPropertiesChanged?.Invoke(playerId, diff);
                    Debug.Assert(item.Kind == ShimItemKind.PlayerPropertiesChanged);
                }
                else
                {
                    if (!OtherPlayers.TryGetValue(playerId, out var player))
                    {
                        _logger.LogError("Received initial state for player {0}", playerId);
                        OtherPlayers[playerId] = new Player(changes.Collection
                            .Where(x => x.Value != null)
                            .ToDictionary(x => x.Key, x => x.Value!));
                        OnPlayerPropertiesAdded?.Invoke(playerId, changes.Collection);
                        Debug.Assert(item.Kind == ShimItemKind.PlayerPropertiesAdded);
                    }
                    else
                    {
                        var diff = RelaySerializer.UpdateAndGetDiff(player.Properties, changes.Collection);
                        OnPlayerPropertiesChanged?.Invoke(playerId, diff);
                        Debug.Assert(item.Kind == ShimItemKind.PlayerPropertiesChanged);
                    }
                }
                break;
            }
            case ShimItemKind.RoomPropertiesChanged:
            {
                if (!InRoom)
                    return false;

                var changes = item.Changes;
                var diff = RelaySerializer.UpdateAndGetDiff(RoomState, changes.Collection);
                OnRoomPropertiesChanged?.Invoke(diff);
                break;
            }
            case ShimItemKind.JoinedRoom:
            {
                if (!_requestedEnterRoom)
                    return false;
                
                var playerId = item.PlayerId;
                var initialState = item.InitialState;
                var newPlayer = new Player(initialState.Collection);

                Debug.Assert(playerId == LocalPlayer.PlayerId);
                LocalPlayer = newPlayer;
                OnBeforeJoinedRoom?.Invoke();
                InRoom = true;
                OnAfterJoinedRoom?.Invoke(initialState.Collection);
                break;
            }
            case ShimItemKind.OtherPlayerJoinedRoom:
            {
                if (!InRoom)
                    return false;
                
                var playerId = item.PlayerId;
                var initialState = item.InitialState;
                var newPlayer = new Player(initialState.Collection);

                Debug.Assert(playerId != LocalPlayer.PlayerId);
                if (!OtherPlayers.TryAdd(playerId, newPlayer))
                {
                    _logger.LogError("Player {0} already exists", playerId);
                    OtherPlayers[playerId] = newPlayer;
                }

                OnOtherPlayerJoined?.Invoke(playerId, initialState.Collection);
                break;
            }
            case ShimItemKind.OtherPlayerLeft:
            {
                if (!InRoom)
                    return false;

                var playerId = item.PlayerId;
                OnOtherPlayerLeft?.Invoke(playerId);
                break;
            }
            case ShimItemKind.EcsDelta:
            {
                if (!InRoom)
                    return false;
                
                var reader = new NetDataReader(item.RawData.Data, item.RawData.Offset, item.RawData.Length);
                OnEcsDelta?.Invoke(reader);
                break;
            }
            case ShimItemKind.EcsSnapshot:
            {
                if (!InRoom)
                    return false;
                
                var reader = new NetDataReader(item.RawData.Data, item.RawData.Offset, item.RawData.Length);
                OnEcsSnapshot?.Invoke(reader);
                break;
            }
            case ShimItemKind.ReceivedDestroyEntity:
            {
                if (!InRoom)
                    return false;
                
                var netId = item.NetworkId;
                OnReceivedDeleteEntity?.Invoke(netId);
                break;
            }
            case ShimItemKind.BlobAck:
            {
                var requestId = item.BlobRequestId;
                var success = item.BlobAckResult;

                if (!_blobUploadTasks.ContainsKey(requestId))
                {
                    _logger.LogInformation("Delaying file upload with request ID {RequestId} completed with success: {Success}", requestId, success);
                    return false;
                }
                    
                _logger.LogInformation("File upload with request ID {RequestId} completed with success: {Success}", requestId, success);

                _blobUploadTasks.TryGetValue(requestId, out var uploadTask);
                if (uploadTask != null)
                {
                    if (success)
                    {
                        if (uploadTask.TrySetResult(true))
                        {
                            _blobUploadTasks.TryRemove(requestId, out _);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to set result for file upload task with request ID {RequestId}", requestId);
                        }
                    }
                    else
                    {
                        if (uploadTask.TrySetException(new Exception("File upload failed")))
                        {
                            _blobUploadTasks.TryRemove(requestId, out _);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to set exception for file upload task with request ID {RequestId}", requestId);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No task found for request ID {RequestId} when receiving upload ack", requestId);
                }

                break;
            }
            case ShimItemKind.BlobData:
            {
                var requestId = item.BlobRequestId;
                var blobData = item.BlobData;

                if (!_blobDownloadTasks.ContainsKey(requestId))
                    return false;

                _logger.LogInformation("Received file stream for {FileName} with request ID {RequestId}", blobData?.Name ?? "<empty>", requestId);
                
                _blobDownloadTasks.TryGetValue(requestId, out var tcs);
                if (tcs != null)
                {
                    if (tcs.TrySetResult(blobData))
                    {
                        _blobDownloadTasks.TryRemove(requestId, out _);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to set result for file download task with request ID {RequestId}", requestId);
                    }
                }
                else
                {
                    _logger.LogWarning("No task found for request ID {RequestId} when receiving file stream for {FileName}", requestId, blobData.Name);
                }

                break;
            }
            case ShimItemKind.CustomEvent:
            {
                if (!InRoom)
                    return false;

                var ev = item.EventHeader;
                var rawData = item.RawData;
                var reader = new NetDataReader(rawData.Data, rawData.Offset, rawData.Length);
                var customEventHandler = _customEventHandlers[ev.EventCode];
                customEventHandler?.Invoke(ev, reader);
                break;
            }
            case ShimItemKind.Disconnected:
            {
                var reason = item.DisconnectReason;
                InRoom = false;
                Connected = false;
                OnDisconnected?.Invoke(reason);
                break;
            }
            case ShimItemKind.PingUpdated:
            {
                OnPingUpdated?.Invoke(item.Ping);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(item.Kind), item.Kind, $"Unknown ShimItemKind: {item.Kind}");
        }

        return true;
    }

    public async Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException();
        
        ct.ThrowIfCancellationRequested();

        var tcs = new TaskCompletionSource<BlobInfo?>();
        var requestId = GetNextRequestId();
        _blobDownloadTasks[requestId] = tcs;

        var writer = new NetDataWriter();
        writer.Put((byte)SystemEvent.DownloadBlob);
        writer.Put(requestId);
        writer.Put(name);
        SendMessageToServer(writer, DeliveryMethod.ReliableOrdered);
        _logger.LogInformation("Requesting file download: {FileName} with request ID {RequestId}", name, requestId);

        using (ct.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false))
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                return await tcs.Task.ConfigureAwait(false);
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
        writer.PutBytesWithLength(blob.Content);
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
    
    // FIXME: Move this to game-specific code
    public void EnterRoom()
    {
        if (!Connected)
            throw new InvalidOperationException("Cannot enter room when not connected");
        
        OnEnterRoomRequest?.Invoke();
        SendInitialPlayerState();
    }

    // FIXME: Move this to game-specific code
    public void ExitRoom()
    {
        if (!Connected)
            throw new InvalidOperationException("Cannot exit room when not connected");
        
        lock (_lock)
        {
            _requestedEnterRoom = false;
        }
        
        ProcessPendingItems();
        
        OnExitRoomRequest?.Invoke();
    }
}