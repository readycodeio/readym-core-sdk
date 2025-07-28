using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Shim;

namespace ReadyM.Relay.Client.Shim;

public class ShimRelayClient : IRelayClient
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

    private readonly ILogger _logger;
    
    private ShimRecording? _recording;
    private int _delay;
    private Stopwatch? _stopwatch;
    private int _index;

    private readonly object _lock = new();
    private readonly List<ShimRequestItem> _requests = new();

    private readonly NetworkThreadContext _netThreadContext = new();
    private readonly PendingActionUpdater<IRelayClientNetworkThreadContext> _scheduler;
    
    public bool IsRunning { get; private set; }

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

    private DisconnectReason _lastDisconnectReason;

    public DisconnectReason LastDisconnectReason
    {
        get
        {
            if (IsRunning)
                throw new InvalidOperationException("Call `Stop()` before safely reading this field.");
            return _lastDisconnectReason;
        }
    }
    
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
    public event Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader>? OnAnyMessage;

    private readonly Action<CustomEventHeader, NetDataReader>?[] _messageHandlers =
        new Action<CustomEventHeader, NetDataReader>?[(int)RelayMessageCode.MaxCustomEvent + 1];

    public ShimRelayClient(ILogger logger)
    {
        _logger = logger;
        _scheduler = new(_netThreadContext, _logger);
    }

    public void AddMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader> callback)
    {
        _messageHandlers[(byte)eventCode] = (Action<CustomEventHeader, NetDataReader>?)Delegate.Combine(_messageHandlers[(byte)eventCode], callback);
    }

    public void AddMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader> callback)
    {
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _messageHandlers[(byte)i] = (Action<CustomEventHeader, NetDataReader>?)Delegate.Combine(_messageHandlers[(byte)i], callback);
        }
    }

    public void RemoveMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader> callback)
    {
        _messageHandlers[(byte)eventCode] = (Action<CustomEventHeader, NetDataReader>?)Delegate.Remove(_messageHandlers[(byte)eventCode], callback);
    }

    public void RemoveMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, CustomEventHeader, NetDataReader> callback)
    {
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _messageHandlers[(byte)i] = (Action<CustomEventHeader, NetDataReader>?)Delegate.Remove(_messageHandlers[(byte)i], callback);
        }
    }

    public event Action<IRelayClientNetworkThreadContext>? OnClientUpdate;

    public void Dispose()
    {
        Stop();
    }

    private bool PeekRequest(out ShimRequestItem requestItem)
    {
        lock (_lock)
        {
            if (_requests.Count == 0)
            {
                requestItem = default;
                return false;
            }
        
            requestItem = _requests[0];
            return true;
        }
    }

    private ShimRequestItem PopRequest()
    {
        lock (_lock)
        {
            var result = _requests[0];
            _requests.RemoveAt(0);
            return result;
        }
    }

    private void PushRequest(ShimRequestItem requestItem)
    {
        lock (_lock)
        {
            _requests.Add(requestItem);
        }
    }
    
    public void SetRecording(ShimRecording recording, int delay = 1_000)
    {
        if (IsRunning)
            throw new InvalidOperationException("Cannot set recording while the client is running");
        _delay = delay;
        lock (_lock)
        {
            _recording = recording;
        }
    }

    public PendingActionScheduler<IRelayClientNetworkThreadContext> Scheduler
    {
        get
        {
            if (!IsRunning)
                throw new InvalidOperationException("Scheduler is only available when the client is running");
            return _scheduler;
        }
    }

    public int GetMaxPacketSize(DeliveryMethod deliveryMethod)
        => 1300;

    private async Task ProcessLoop(CancellationToken token)
    {
        try
        {
            ShimItem? item = null;
            lock (_lock)
            {
                if (_index < _recording!.Items.Count)
                {
                    item = _recording.Items[_index];
                }
            }

            if (item == null)
            {
                await Task.Delay(Constants.ClientNetworkTickRateMs, token);
                goto finish;
            }

            var elapsed = _stopwatch!.ElapsedMilliseconds - _delay;

            if (item.Value.Elapsed > elapsed)
            {
                await Task.Delay(Constants.ClientNetworkTickRateMs, token);
                goto finish;
            }

            _index++;
            ProcessItem(item.Value);
            
            finish:
            {
                OnClientUpdate?.Invoke(_netThreadContext);

                var hadPendingActions = _scheduler.Update();
                if (!hadPendingActions)
                {
                    await Task.Delay(Constants.ClientNetworkTickRateMs, token);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in client thread (starting)");
        }
    }

    public async Task StartAsync(CancellationToken token, bool autoConnect = true)
    {
        if (_recording == null)
            throw new InvalidOperationException("Recording is not set. Call `SetRecording()` before starting the client.");
        
        if (IsRunning)
        {
            _logger.LogError("Relay client is already running");
            return;
        }
        
        OnRequestedStart?.Invoke();

        await Task.Delay(1, token);
        _scheduler.SetThread(Thread.CurrentThread);
        
        if (autoConnect)
        {
            PushRequest(new ShimRequestItem()
            {
                Kind = ShimRequestKind.RequestedConnect,
            });
        }

        IsRunning = true;

        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        _index = 0;

        _logger.LogInformation("Starting shim relay client");
        
        while (!token.IsCancellationRequested)
        {
            await ProcessLoop(token);
            
            if (_netThreadContext.Connected)
                break;
        }
        
        _logger.LogInformation("Shim relay client started successfully");
    }

    public async Task RunAsync(CancellationToken token)
    {
        if (_recording == null)
            throw new InvalidOperationException("Recording is not set. Call `SetRecording()` before starting the client.");

        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running. Call `StartAsync()` first.");
            return;
        }

        _logger.LogInformation("Running shim relay client");
        
        while (!token.IsCancellationRequested)
        {
            await ProcessLoop(token);
        }
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            _logger.LogInformation("Shim relay client is not running");
            return;
        }
        
        IsRunning = false;
        
        _logger.LogDebug("Stopping shim relay client");

        OnRequestedStop?.Invoke();

        // NOTE: It is possible that the client requests a disconnect, and simultaneously the server disconnects
        // from the client forcefully. In that case the corresponding `OnDisconnected` event will not be fired.
        if (LastDisconnectReason != DisconnectReason.DisconnectPeerCalled)
        {
            _logger.LogWarning("Shim relay client already disconnected: {Reason}", LastDisconnectReason);
        }

        _logger.LogDebug("Stopped shim relay client");
    }

    public void Connect()
    {
        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }
        
        PushRequest(new ShimRequestItem()
        {
            Kind = ShimRequestKind.RequestedConnect,
        });
        
        OnRequestedConnect?.Invoke();
    }

    public void Disconnect()
    {
        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }
        
        PushRequest(new ShimRequestItem()
        {
            Kind = ShimRequestKind.RequestedDisconnect,
        });
        
        OnRequestedDisconnect?.Invoke();
    }

    public void Reconnect()
    {
        Disconnect();
        Connect();
    }

    public void JoinArea(AreaId areaId)
    {
        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }
        
        PushRequest(new ShimRequestItem()
        {
            Kind = ShimRequestKind.RequestedJoinArea,
            AreaId = areaId,
        });
        
        OnRequestedJoinArea?.Invoke(areaId);
    }

    public void LeaveArea()
    {
        if (!IsRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }
        
        PushRequest(new ShimRequestItem()
        {
            Kind = ShimRequestKind.RequestedLeaveArea,
        });
        
        OnRequestedLeaveArea?.Invoke();
    }

    public void SendRawMessage(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        _logger.LogTrace("Sending message to server with delivery method {DeliveryMethod}", deliveryMethod);
    }

    public void SendMessage(RelayMessage message)
    {
        _logger.LogTrace("Sending message to server: {Message}", message);
    }

    public void SendMessageToServer<T>(RelayMessageCode eventCode, T data, DeliveryMethod deliveryMethod) where T : INetSerializable
    {
        _logger.LogTrace("Sending message to server: {EventCode} with data {Data}", eventCode, data);
    }

    public void SendMessageToPeers<T>(RelayMessageCode eventCode, T data, PlayerId[] peers, DeliveryMethod deliveryMethod) where T : INetSerializable
    {
        _logger.LogTrace("Sending message to peers: {EventCode} with data {Data} to players {Peers}", eventCode, data, string.Join(", ", peers));
    }

    public void SendMessageRelayMode<T>(RelayMessageCode eventCode, T data, RelayMode mode, DeliveryMethod deliveryMethod) where T : INetSerializable
    {
        _logger.LogTrace("Sending message to peers in relay mode {Mode}: {EventCode} with data {Data}", mode, eventCode, data);
    }

    public bool ProcessItem(ShimItem item)
    {
        switch (item.Kind)
        {
            case ShimItemKind.RequestedConnect:
            {
                if (!PeekRequest(out var request) || request.Kind != ShimRequestKind.RequestedConnect)
                    return false;
                PopRequest();
                break;
            }
            case ShimItemKind.Connected:
            {
                // Assumes RequestedStart first
                var playerId = item.PlayerId;
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
            case ShimItemKind.RequestedDisconnect:
            {
                if (!PeekRequest(out var request) || request.Kind != ShimRequestKind.RequestedDisconnect)
                    return false;
                PopRequest();
                break;
            }
            case ShimItemKind.Disconnected:
            {
                // Assumes RequestedStop first
                _logger.LogInformation("Disconnected from server: {Reason}", item.DisconnectReason);
                _netThreadContext.Connected = false;
                _netThreadContext.LastDisconnectReason = item.DisconnectReason;
                _lastDisconnectReason = item.DisconnectReason;
                OnDisconnected?.Invoke(_netThreadContext, item.DisconnectReason);
                break;
            }
            case ShimItemKind.OtherPlayerConnected:
            {
                var playerId = item.PlayerId;
                if (!_netThreadContext.AllPlayers.Contains(playerId))
                {
                    _netThreadContext.AllPlayers.Add(playerId);
                    OnOtherPlayerConnected?.Invoke(_netThreadContext, playerId);
                }
                else
                {
                    _logger.LogError("Player connected event for player {PlayerId} that already is marked as connected", playerId);
                }
                break;
            }
            case ShimItemKind.OtherPlayerDisconnected:
            {
                var playerId = item.PlayerId;
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
                break;
            }
            case ShimItemKind.RequestedJoinArea:
            {
                if (!PeekRequest(out var request) || request.Kind != ShimRequestKind.RequestedJoinArea)
                    return false;
                PopRequest();
                break;
            }
            case ShimItemKind.JoinedArea:
            {
                // Assumes RequestedJoinArea first
                var playerId = item.PlayerId;
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
                var areaId = item.AreaId;
                _netThreadContext.CurrentArea = areaId;
                _netThreadContext.AreaPlayers.Clear();
                _netThreadContext.AreaPlayers.Add(playerId);
                OnJoinedArea?.Invoke(_netThreadContext, areaId);
                break;
            }
            case ShimItemKind.RequestedLeaveArea:
            {
                if (!PeekRequest(out var request) || request.Kind != ShimRequestKind.RequestedLeaveArea)
                    return false;
                PopRequest();
                break;
            }
            case ShimItemKind.LeftArea:
            {
                // Assumes RequestedLeaveArea first
                var playerId = item.PlayerId;
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
                break;
            }
            case ShimItemKind.OtherPlayerJoinedArea:
            {
                var playerId = item.PlayerId;
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
                break;
            }
            case ShimItemKind.OtherPlayerLeftArea:
            {
                var playerId = item.PlayerId;
                if (_netThreadContext.AreaPlayers.Contains(playerId))
                {
                    _netThreadContext.AreaPlayers.Remove(playerId);
                    OnOtherPlayerLeftArea?.Invoke(_netThreadContext, playerId);
                }
                else
                {
                    _logger.LogError("Player left area event for player {PlayerId} that already is marked as NOT in the area", playerId);
                }
                break;
            }
            case ShimItemKind.PingUpdated:
            {
                OnPingUpdated?.Invoke(_netThreadContext, item.Ping);
                break;
            }
            case ShimItemKind.AnyMessage:
            {
                if (!_netThreadContext.Connected)
                    return false;
                
                var ev = item.EventHeader;
                if (ev.RelayMode == RelayMode.AreaOfInterestOthers || ev.RelayMode == RelayMode.AreaOfInterestAll)
                {
                    if (_netThreadContext.CurrentArea == AreaId.Invalid)
                        return false;
                }
                
                var rawData = item.RawData;
                var reader = new NetDataReader(rawData.Data, rawData.Offset, rawData.Length);
                var customEventHandler = _messageHandlers[ev.EventCode];
                customEventHandler?.Invoke(ev, reader);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(item.Kind), item.Kind, $"Unknown ShimItemKind: {item.Kind}");
        }

        return true;
    }
}