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
        public PlayerId? PlayerId { get; set; }
        public AreaId? CurrentArea { get; set; }
        
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

    private volatile bool _isRunning;
    private volatile bool _isPlaying;

    public bool IsPlaying
        => _isPlaying;
    
    public bool RequestedConnect { get; private set; }
    public AreaId? RequestedAreaId { get; private set; }

    public PlayerId? PlayerId
    {
        get
        {
            if (!RequestedConnect)
                return null;
            lock (_lock)
            {
                return _recording!.PlayerId;
            }
        }
    }

    private DisconnectReason _lastDisconnectReason;

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
        
        _serverMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)eventCode], handler);
    }

    public void AddBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinBuiltInEvent || minEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        if (maxEventCode < RelayMessageCode.MinBuiltInEvent || maxEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)i], handler);
        }
    }

    public void RemoveBuiltInMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinBuiltInEvent || eventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Remove(_clientMessageHandlers[(byte)eventCode], handler);
    }

    public void RemoveBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinBuiltInEvent || minEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        if (maxEventCode < RelayMessageCode.MinBuiltInEvent || maxEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");

        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)i], handler);
        }
    }

    public void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinServerRpcEvent || eventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinServerRpcEvent)}` and `{nameof(RelayMessageCode.MaxServerRpcEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)eventCode], handler);
    }

    public void AddServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinServerRpcEvent || minEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        if (maxEventCode < RelayMessageCode.MinServerRpcEvent || maxEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)i], handler);
        }
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinServerRpcEvent || eventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinServerRpcEvent)}` and `{nameof(RelayMessageCode.MaxServerRpcEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Remove(_clientMessageHandlers[(byte)eventCode], handler);
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinServerRpcEvent || minEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        if (maxEventCode < RelayMessageCode.MinServerRpcEvent || maxEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<IRelayClientNetworkThreadContext, ServerEventHeader, NetDataReader>?)Delegate.Remove(_clientMessageHandlers[(byte)i], handler);
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

    public ShimRelayClient(ILogger logger)
    {
        _logger = logger;
        _scheduler = new(_netThreadContext, _logger);
    }
    
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
        if (_isRunning)
            throw new InvalidOperationException("Cannot set recording while the client is running");
        _delay = delay;
        lock (_lock)
        {
            _recording = recording;
        }
    }

    public PendingActionScheduler<IRelayClientNetworkThreadContext> Scheduler
        => _scheduler;

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
    }

    public void StopPlaying()
    {
        if (!_isPlaying)
        {
            _logger.LogError("Shim relay client is not playing");
            return;
        }

        _isPlaying = false;
    }
    
    public void Start()
    {
        if (_recording == null)
            throw new InvalidOperationException("Recording is not set. Call `SetRecording()` before starting the client.");
        
        if (_isRunning)
        {
            _logger.LogError("Relay client is already running");
            return;
        }
        _isRunning = true;

        _logger.LogInformation("Starting shim relay client...");

        OnStart?.Invoke();

        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        _index = 0;

        _logger.LogInformation("Started shim relay client");
    }

    public async Task RunAsync(CancellationToken token)
    {
        if (_recording == null)
            throw new InvalidOperationException("Recording is not set. Call `SetRecording()` before starting the client.");

        if (!_isRunning)
        {
            _logger.LogError("Relay client is not running. Call `StartAsync()` first.");
            return;
        }

        _logger.LogInformation("Running shim relay client");
        
        while (!token.IsCancellationRequested)
        {
            if (!_isPlaying)
            {
                await Task.Delay(Constants.ClientNetworkTickRateMs, token);
                continue;
            }
            
            await ProcessLoop(token);
        }
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            _logger.LogInformation("Shim relay client is not running");
            return;
        }
        
        _isRunning = false;
        _scheduler.SetThread(null);

        _logger.LogDebug("Stopping shim relay client");

        OnRequestedStop?.Invoke();

        // NOTE: It is possible that the client requests a disconnect, and simultaneously the server disconnects
        // from the client forcefully. In that case the corresponding `OnDisconnected` event will not be fired.
        if (_lastDisconnectReason != DisconnectReason.DisconnectPeerCalled)
        {
            _logger.LogWarning("Shim relay client already disconnected: {Reason}", _lastDisconnectReason);
        }

        _logger.LogDebug("Stopped shim relay client");
    }

    public void RequestConnect()
    {
        if (!_isRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }

        if (RequestedConnect)
        {
            _logger.LogWarning("Relay client is already connecting");
            return;
        }
        RequestedConnect = true;
        
        PushRequest(new ShimRequestItem()
        {
            Kind = ShimRequestKind.RequestedConnect,
        });
        
        OnRequestedConnect?.Invoke();
    }

    public void RequestDisconnect()
    {
        if (!_isRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }
        
        if (!RequestedConnect)
        {
            _logger.LogWarning("Relay client is already disconnecting");
            return;
        }
        RequestedConnect = false;
        
        PushRequest(new ShimRequestItem()
        {
            Kind = ShimRequestKind.RequestedDisconnect,
        });
        
        OnRequestedDisconnect?.Invoke();
    }

    public void RequestReconnect()
    {
        RequestDisconnect();
        RequestConnect();
    }

    public void RequestJoinArea(AreaId areaId)
    {
        if (!_isRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }

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
        
        PushRequest(new ShimRequestItem()
        {
            Kind = ShimRequestKind.RequestedJoinArea,
            AreaId = areaId,
        });
        
        OnRequestedJoinArea?.Invoke(areaId);
    }

    public void RequestLeaveArea()
    {
        if (!_isRunning)
        {
            _logger.LogError("Relay client is not running");
            return;
        }

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
                if (_netThreadContext.PlayerId != null)
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
            case ShimItemKind.AnyServerMessage:
            {
                if (!_netThreadContext.Connected)
                    return false;
                
                var serverHeader = item.ServerHeader;
                var rawData = item.RawData;
                var reader = new NetDataReader(rawData.Data, rawData.Offset, rawData.Length);
                var serverHandler = _serverMessageHandlers[serverHeader.EventCode];
                serverHandler?.Invoke(_netThreadContext, serverHeader, reader);
                break;
            }
            case ShimItemKind.AnyClientMessage:
            {
                if (!_netThreadContext.Connected)
                    return false;
                
                var clientHeader = item.ClientHeader;
                if (clientHeader.RelayMode == RelayMode.AreaOfInterestOthers || clientHeader.RelayMode == RelayMode.AreaOfInterestAll)
                {
                    if (_netThreadContext.CurrentArea == AreaId.Invalid)
                        return false;
                }
                
                var rawData = item.RawData;
                var reader = new NetDataReader(rawData.Data, rawData.Offset, rawData.Length);
                var clientHandler = _clientMessageHandlers[clientHeader.EventCode];
                clientHandler?.Invoke(_netThreadContext, clientHeader, reader);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(item.Kind), item.Kind, $"Unknown ShimItemKind: {item.Kind}");
        }

        return true;
    }
}