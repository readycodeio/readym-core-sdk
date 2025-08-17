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
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Shim;

namespace ReadyM.Relay.Client.Shim;

public class ShimPlaybackRelayClient : IRelayClient
{
    private class NetworkThreadContext : IRelayClientNetworkThreadContext
    {
        public readonly List<PlayerId> AllPlayers = new();
        public readonly List<PlayerId> AreaPlayers = new();

        public bool IsConnected { get; set; }
        public PlayerId? PlayerId { get; set; }
        public AreaId? CurrentAreaId { get; set; }
        
        ReadOnlyList<PlayerId> IRelayClientNetworkThreadContext.AllPlayers
            => new(AllPlayers);
        
        ReadOnlyList<PlayerId> IRelayClientNetworkThreadContext.AreaPlayers
            => new(AreaPlayers);
    }

    private readonly ShimReplayDependencyTracker _depTracker;
    private readonly ShimRelayMessageParser _parser;
    private readonly ILogger _logger;
    
    private int _delay;
    private Stopwatch? _stopwatch;

    private readonly object _lock = new();

    private PlayerId? _playerId;
    private List<ShimRequestItem> _requestItems = new();
    private int _responseItemIndex;
    private List<ShimResponseItem> _responseItems = new();

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
                return _playerId;
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

    public ShimPlaybackRelayClient(ShimReplayDependencyTracker depTracker, ShimRelayMessageParser parser, ILogger logger)
    {
        _depTracker = depTracker;
        _parser = parser;
        _logger = logger;
        _scheduler = new(_netThreadContext, _logger);
    }
    
    public void Dispose()
    {
        Stop();
    }

    private void AddRequest(ShimRequestItem requestItem)
    {
        lock (_lock)
        {
            _requestItems.Add(requestItem);
        }
    }
    
    public void SetRecording(ShimRecording recording, int delay = 1_000)
    {
        if (_isRunning)
            throw new InvalidOperationException("Cannot set recording while the client is running");
        _delay = delay;
        lock (_lock)
        {
            _playerId = recording.PlayerId;
            _requestItems = new();
            _responseItems = new List<ShimResponseItem>(recording.ResponseItems);
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
            ShimResponseItem? responseItem = null;
            lock (_lock)
            {
                if (_responseItemIndex < _responseItems.Count)
                {
                    responseItem = _responseItems[_responseItemIndex];
                }
            }

            if (responseItem == null)
            {
                await Task.Delay(Constants.ClientNetworkTickRateMs, token);
                goto finish;
            }

            var elapsed = _stopwatch!.ElapsedMilliseconds - _delay;

            if (responseItem.Value.Elapsed > elapsed)
            {
                await Task.Delay(Constants.ClientNetworkTickRateMs, token);
                goto finish;
            }

            if (ProcessResponseItem(responseItem.Value))
            {
                _responseItemIndex++;

                for (var i = 0; i < _requestItems.Count;)
                {
                    var requestItem = _requestItems[i];
                    if (_depTracker.CheckRequestCanDelete(requestItem, responseItem.Value))
                    {
                        _requestItems.RemoveAt(i);
                        continue;
                    }

                    i++;
                }
            }

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
        _responseItemIndex = 0;

        _logger.LogInformation("Started shim relay client");
    }

    public async Task RunAsync(CancellationToken token)
    {
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
        
        AddRequest(new ShimRequestItem()
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
        
        AddRequest(new ShimRequestItem()
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
        
        AddRequest(new ShimRequestItem()
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
        
        AddRequest(new ShimRequestItem()
        {
            Kind = ShimRequestKind.RequestedLeaveArea,
        });
        
        OnRequestedLeaveArea?.Invoke();
    }

    [ThreadStatic] private static NetDataReader? _reader;

    public void SendRawMessage(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        var reader = _reader ??= new NetDataReader();
        reader.SetSource(writer.Data, 0, writer.Length);
     
        var eventCode = (RelayMessageCode)reader.GetByte();
        var requestItem = new ShimRequestItem();
        
        if (eventCode >= RelayMessageCode.MinBuiltInEvent)
        {
            var serverHeader = new ServerEventHeader(eventCode, Api.Multiplayer.Idents.PlayerId.Server);
            requestItem.Kind = ShimRequestKind.SentBuiltInMessage;
            requestItem.ServerHeader = serverHeader;
            requestItem.CustomData = _parser.GetBuiltInRequestCustomData(serverHeader, reader);
        }
        else if (eventCode >= RelayMessageCode.MinServerRpcEvent)
        {
            var serverHeader = new ServerEventHeader(eventCode, Api.Multiplayer.Idents.PlayerId.Server);
            requestItem.Kind = ShimRequestKind.SentServerRpcMessage;
            requestItem.ServerHeader = serverHeader;
            requestItem.CustomData = _parser.GetServerRpcRequestCustomData(serverHeader, reader);
        }
        else
        {
            var clientHeader = reader.GetCustomRelayEventHeader(eventCode);
            requestItem.Kind = ShimRequestKind.SentClientRpcMessage;
            requestItem.ClientHeader = clientHeader;
            requestItem.CustomData = _parser.GetClientRpcRequestCustomData(clientHeader, reader);
        }

        requestItem.RawData = new ShimBuffer(writer.Data, 1, writer.Length);
        
        AddRequest(requestItem);
    }

    public void SendMessage(RelayMessage message)
    {
        SendRawMessage(message.Writer, message.DeliveryMethod);
    }

    public void SendMessageToServer<T>(RelayMessageCode eventCode, T data, DeliveryMethod deliveryMethod)
        where T : INetSerializable
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

    public bool ProcessResponseItem(ShimResponseItem responseItem)
    {
        if (_depTracker.CheckResponseShouldWait(responseItem, _netThreadContext, _requestItems))
            return false;
        
        switch (responseItem.Kind)
        {
            case ShimResponseKind.Connected:
            {
                // Assumes RequestedStart first
                var playerId = responseItem.PlayerId;
                if (_netThreadContext.PlayerId != null)
                {
                    _logger.LogError("Missing handshake for player {PlayerId} but already assigned {AssignedPlayerId}", playerId, _netThreadContext.PlayerId);
                }
                _netThreadContext.IsConnected = true;
                _netThreadContext.PlayerId = playerId;
                _netThreadContext.AllPlayers.Add(playerId);
                _logger.LogInformation("Assigned Actor ID {PlayerId}", playerId);
                OnConnected?.Invoke(_netThreadContext, playerId);
                break;
            }
            case ShimResponseKind.Disconnected:
            {
                // Assumes RequestedStop first
                _logger.LogInformation("Disconnected from server: {Reason}", responseItem.DisconnectReason);
                _netThreadContext.IsConnected = false;
                _lastDisconnectReason = responseItem.DisconnectReason;
                OnDisconnected?.Invoke(_netThreadContext, responseItem.DisconnectReason);
                break;
            }
            case ShimResponseKind.OtherPlayerConnected:
            {
                if (!_netThreadContext.IsConnected)
                    return false;  

                var playerId = responseItem.PlayerId;
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
            case ShimResponseKind.OtherPlayerDisconnected:
            {
                if (!_netThreadContext.IsConnected)
                    return false;  

                var playerId = responseItem.PlayerId;
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
            case ShimResponseKind.JoinedArea:
            {
                if (!_netThreadContext.IsConnected)
                    return false;  

                // Assumes RequestedJoinArea first
                var playerId = responseItem.PlayerId;
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
                var areaId = responseItem.AreaId;
                _netThreadContext.CurrentAreaId = areaId;
                _netThreadContext.AreaPlayers.Clear();
                _netThreadContext.AreaPlayers.Add(playerId);
                OnJoinedArea?.Invoke(_netThreadContext, areaId);
                break;
            }
            case ShimResponseKind.LeftArea:
            {
                if (!_netThreadContext.IsConnected)
                    return false;
                if (_netThreadContext.CurrentAreaId == null)
                    return false;

                // Assumes RequestedLeaveArea first
                var playerId = responseItem.PlayerId;
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
                
                _netThreadContext.CurrentAreaId = null;
                _netThreadContext.AreaPlayers.Remove(playerId);
                OnLeftArea?.Invoke(_netThreadContext);
                break;
            }
            case ShimResponseKind.OtherPlayerJoinedArea:
            {
                if (!_netThreadContext.IsConnected)
                    return false;
                if (_netThreadContext.CurrentAreaId == null)
                    return false;
                
                var playerId = responseItem.PlayerId;
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
                break;
            }
            case ShimResponseKind.OtherPlayerLeftArea:
            {
                if (!_netThreadContext.IsConnected)
                    return false;
                if (_netThreadContext.CurrentAreaId == null)
                    return false;

                var playerId = responseItem.PlayerId;
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
            case ShimResponseKind.PingUpdated:
            {
                if (!_netThreadContext.IsConnected)
                    return false;
                
                OnPingUpdated?.Invoke(_netThreadContext, responseItem.Ping);
                break;
            }
            case ShimResponseKind.AnyBuiltInMessage:
            {
                if (!_netThreadContext.IsConnected)
                    return false;  
                
                var serverHeader = responseItem.ServerHeader;
                var rawData = responseItem.RawData;
                var reader = new NetDataReader(rawData.Data, rawData.Offset, rawData.MaxSize);
                var serverHandler = _serverMessageHandlers[(int)serverHeader.EventCode];
                serverHandler?.Invoke(_netThreadContext, serverHeader, reader);
                break;
            }
            case ShimResponseKind.AnyServerMessage:
            {
                if (!_netThreadContext.IsConnected)
                    return false;
                
                var serverHeader = responseItem.ServerHeader;
                var rawData = responseItem.RawData;
                var reader = new NetDataReader(rawData.Data, rawData.Offset, rawData.MaxSize);
                var serverHandler = _serverMessageHandlers[(int)serverHeader.EventCode];
                serverHandler?.Invoke(_netThreadContext, serverHeader, reader);
                break;
            }
            case ShimResponseKind.AnyClientMessage:
            {
                if (!_netThreadContext.IsConnected)
                    return false;
                
                var clientHeader = responseItem.ClientHeader;
                if (clientHeader.RelayMode == RelayMode.AreaOfInterestOthers || clientHeader.RelayMode == RelayMode.AreaOfInterestAll)
                {
                    if (_netThreadContext.CurrentAreaId == null)
                        return false;
                }
                
                var rawData = responseItem.RawData;
                var reader = new NetDataReader(rawData.Data, rawData.Offset, rawData.MaxSize);
                var clientHandler = _clientMessageHandlers[(int)clientHeader.EventCode];
                clientHandler?.Invoke(_netThreadContext, clientHeader, reader);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(responseItem.Kind), responseItem.Kind, $"Unknown ShimItemKind: {responseItem.Kind}");
        }

        return true;
    }
}