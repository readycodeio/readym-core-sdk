using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Relay.Client;

internal class HotSwappableRelayClient : IRelayClient
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
            if (_client.RequestedConnect)
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
            if (_client.RequestedConnect)
                throw new InvalidOperationException("Cannot swap RelayClient while it is connected. Please stop the client first.");
                
            OnRelayClientDetach?.Invoke(_client);
            DetachRelayClient(_client);
        }

        _client = null;
    }

    private void AttachRelayClient(IRelayClient client)
    {
        client.OnStart += OnRequestedStartHandler;
        client.OnRequestedStop += OnRequestedRequestedStopHandler;
        client.OnRequestedConnect += OnRequestedConnectHandler;
        client.OnConnected += OnConnectedHandler;
        client.OnRequestedDisconnect += OnRequestedDisconnectHandler;
        client.OnDisconnected += OnDisconnectedHandler;
        client.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        client.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        client.OnRequestedJoinArea += OnRequestedJoinAreaHandler;
        client.OnRequestedSetActiveCells += OnRequestedSetActiveCellsEvent;
        client.OnJoinedArea += OnJoinedAreaHandler;
        client.OnRequestedLeaveArea += OnRequestedLeaveAreaHandler;
        client.OnLeftArea += OnLeftAreaHandler;
        client.OnActiveCellsSet += OnActiveCellsSetHandler;
        client.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        client.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        client.OnPingUpdated += OnPingUpdatedHandler;
        client.OnAnyBuiltInMessage += OnAnyBuiltInMessageHandler;
        client.OnAnyServerRpcMessage += OnAnyServerRpcMessageHandler;
        client.OnAnyClientRpcMessage += OnAnyClientRpcMessageHandler;
        client.OnClientUpdate += OnClientUpdateHandler;
    }

    private void DetachRelayClient(IRelayClient client)
    {
        client.OnClientUpdate -= OnClientUpdateHandler;
        client.OnAnyClientRpcMessage -= OnAnyClientRpcMessageHandler;
        client.OnAnyServerRpcMessage -= OnAnyServerRpcMessageHandler;
        client.OnAnyBuiltInMessage -= OnAnyBuiltInMessageHandler;
        client.OnPingUpdated -= OnPingUpdatedHandler;
        client.OnOtherPlayerLeftArea -= OnOtherPlayerLeftAreaHandler;
        client.OnOtherPlayerJoinedArea -= OnOtherPlayerJoinedAreaHandler;
        client.OnActiveCellsSet -= OnActiveCellsSetHandler;
        client.OnLeftArea -= OnLeftAreaHandler;
        client.OnRequestedLeaveArea -= OnRequestedLeaveAreaHandler;
        client.OnRequestedSetActiveCells -= OnRequestedSetActiveCellsEvent;
        client.OnJoinedArea -= OnJoinedAreaHandler;
        client.OnRequestedJoinArea -= OnRequestedJoinAreaHandler;
        client.OnOtherPlayerDisconnected -= OnOtherPlayerDisconnectedHandler;
        client.OnOtherPlayerConnected -= OnOtherPlayerConnectedHandler;
        client.OnDisconnected -= OnDisconnectedHandler;
        client.OnRequestedDisconnect -= OnRequestedDisconnectHandler;
        client.OnConnected -= OnConnectedHandler;
        client.OnRequestedConnect -= OnRequestedConnectHandler;
        client.OnRequestedStop -= OnRequestedRequestedStopHandler;
        client.OnStart -= OnRequestedStartHandler;
    }

    public void Dispose()
    {
        // empty
    }

    public PlayerId? PlayerId
        => _client?.PlayerId;

    public bool RequestedConnect
        => _client?.RequestedConnect ?? false;
    
    public AreaId? RequestedAreaId
        => _client?.RequestedAreaId;
    
    public event Action? OnStart;
    public event Action? OnRequestedStop;

    public event Action? OnRequestedConnect;
    public event Action<IRelayClientNetworkThreadContext, PlayerId, uint>? OnConnected;
    public event Action? OnRequestedDisconnect;
    public event Action<IRelayClientNetworkThreadContext>? OnDisconnected;
    
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerConnected;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerDisconnected;
    public event Action<AreaId>? OnRequestedJoinArea;
    public event Action<ReadOnlyArray<CellId>>? OnRequestedSetActiveCells;
    public event Action<IRelayClientNetworkThreadContext, AreaId>? OnJoinedArea;
    public event Action? OnRequestedLeaveArea;
    public event Action<IRelayClientNetworkThreadContext>? OnLeftArea;
    public event Action<IRelayClientNetworkThreadContext>? OnActiveCellsSet;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerJoinedArea;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerLeftArea;
    public event Action<int>? OnPingUpdated;
    
    public event Action<ServerEventHeader, NetDataReader>? OnAnyBuiltInMessage
    {
        add => AddBuiltInMessageHandler(RelayMessageCode.MinBuiltInEvent, RelayMessageCode.MaxBuiltInEvent, value!);
        remove => RemoveBuiltInMessageHandler(RelayMessageCode.MinBuiltInEvent, RelayMessageCode.MaxBuiltInEvent, value!);
    }

    public event Action<ServerEventHeader, NetDataReader>? OnAnyServerRpcMessage
    {
        add => AddServerRpcMessageHandler(RelayMessageCode.MinServerRpcEvent, RelayMessageCode.MaxServerRpcEvent, value!);
        remove => RemoveServerRpcMessageHandler(RelayMessageCode.MinServerRpcEvent, RelayMessageCode.MaxServerRpcEvent, value!);
    }

    public event Action<CustomRelayEventHeader, NetDataReader>? OnAnyClientRpcMessage
    {
        add => AddClientRpcMessageHandler(RelayMessageCode.MinClientRpcEvent, RelayMessageCode.MaxClientRpcEvent, value!);
        remove => RemoveClientRpcMessageHandler(RelayMessageCode.MinClientRpcEvent, RelayMessageCode.MaxClientRpcEvent, value!);
    }

    private readonly Action<ServerEventHeader, NetDataReader>?[] _serverMessageHandlers =
        new Action<ServerEventHeader, NetDataReader>?[(int)RelayMessageCode.MaxBuiltInEvent + 1];
    private readonly Action<CustomRelayEventHeader, NetDataReader>?[] _clientMessageHandlers =
        new Action<CustomRelayEventHeader, NetDataReader>?[(int)RelayMessageCode.MaxClientRpcEvent + 1];

    public void AddBuiltInMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinBuiltInEvent || eventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)eventCode], handler);
    }

    public void AddBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinBuiltInEvent || minEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        if (maxEventCode < RelayMessageCode.MinBuiltInEvent || maxEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)i], handler);
        }
    }

    public void RemoveBuiltInMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinBuiltInEvent || eventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<ServerEventHeader, NetDataReader>?)Delegate.Remove(_serverMessageHandlers[(byte)eventCode], handler);
    }

    public void RemoveBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinBuiltInEvent || minEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");
        if (maxEventCode < RelayMessageCode.MinBuiltInEvent || maxEventCode > RelayMessageCode.MaxBuiltInEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinBuiltInEvent)}` and `{nameof(RelayMessageCode.MaxBuiltInEvent)}`");

        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)i], handler);
        }
    }

    public void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinServerRpcEvent || eventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinServerRpcEvent)}` and `{nameof(RelayMessageCode.MaxServerRpcEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)eventCode], handler);
    }

    public void AddServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinServerRpcEvent || minEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        if (maxEventCode < RelayMessageCode.MinServerRpcEvent || maxEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<ServerEventHeader, NetDataReader>?)Delegate.Combine(_serverMessageHandlers[(byte)i], handler);
        }
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (eventCode < RelayMessageCode.MinServerRpcEvent || eventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinServerRpcEvent)}` and `{nameof(RelayMessageCode.MaxServerRpcEvent)}`");
        
        _serverMessageHandlers[(byte)eventCode] = (Action<ServerEventHeader, NetDataReader>?)Delegate.Remove(_serverMessageHandlers[(byte)eventCode], handler);
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (minEventCode < RelayMessageCode.MinServerRpcEvent || minEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        if (maxEventCode < RelayMessageCode.MinServerRpcEvent || maxEventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _serverMessageHandlers[(byte)i] = (Action<ServerEventHeader, NetDataReader>?)Delegate.Remove(_serverMessageHandlers[(byte)i], handler);
        }
    }

    public void AddClientRpcMessageHandler(RelayMessageCode eventCode, Action<CustomRelayEventHeader, NetDataReader> handler)
    {
        if (eventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        _clientMessageHandlers[(byte)eventCode] = (Action<CustomRelayEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)eventCode], handler);
    }

    public void AddClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<CustomRelayEventHeader, NetDataReader> handler)
    {
        if (minEventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        if (maxEventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(maxEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _clientMessageHandlers[(byte)i] = (Action<CustomRelayEventHeader, NetDataReader>?)Delegate.Combine(_clientMessageHandlers[(byte)i], handler);
        }
    }

    public void RemoveClientRpcMessageHandler(RelayMessageCode eventCode, Action<CustomRelayEventHeader, NetDataReader> handler)
    {
        if (eventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        _clientMessageHandlers[(byte)eventCode] = (Action<CustomRelayEventHeader, NetDataReader>?)Delegate.Remove(_clientMessageHandlers[(byte)eventCode], handler);
    }

    public void RemoveClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<CustomRelayEventHeader, NetDataReader> handler)
    {
        if (minEventCode > RelayMessageCode.MaxClientRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(minEventCode), $"Event code must be between `{nameof(RelayMessageCode.MinClientRpcEvent)}` and `{nameof(RelayMessageCode.MaxClientRpcEvent)}`");
        
        for (var i = minEventCode; i <= maxEventCode; i++)
        {
            _clientMessageHandlers[(byte)i] = (Action<CustomRelayEventHeader, NetDataReader>?)Delegate.Remove(_clientMessageHandlers[(byte)i], handler);
        }
    }

    public event Action<IRelayClientNetworkThreadContext>? OnClientUpdate;

    public PendingActionScheduler<IRelayClientNetworkThreadContext> Scheduler
        => _client!.Scheduler;

    public int GetMaxPacketSize(DeliveryMethod deliveryMethod)
        => _client?.GetMaxPacketSize(deliveryMethod) ?? 1300;

    public void Start()
        => _client!.Start();

    public Task RunAsync(CancellationToken token)
        => _client!.RunAsync(token);

    public void Stop()
        => _client!.Stop();

    public void RequestConnect()
        => _client!.RequestConnect();

    public void RequestDisconnect()
        => _client!.RequestDisconnect();

    public void RequestReconnect()
        => _client!.RequestReconnect();

    public void RequestJoinArea(AreaId areaId)
        => _client!.RequestJoinArea(areaId);

    public void RequestSetActiveCells(CellId[] cellIds)
        => _client!.RequestSetActiveCells(cellIds);

    public void RequestLeaveArea()
        => _client!.RequestLeaveArea();

    public void SendRawMessage(NetDataWriter writer, DeliveryMethod deliveryMethod)
        => _client!.SendRawMessage(writer, deliveryMethod);

    public void SendMessage(RelayMessage message)
        => _client!.SendMessage(message);

    public void SendMessageToServer<T>(RelayMessageCode eventCode, T data, DeliveryMethod deliveryMethod)
        where T : INetSerializable
        => _client!.SendMessageToServer(eventCode, data, deliveryMethod);

    public void LogEventStats() => _client?.LogEventStats();

    #region Event handlers
    
    private void OnRequestedStartHandler()
        => OnStart?.Invoke();

    private void OnRequestedRequestedStopHandler()
        => OnRequestedStop?.Invoke();

    private void OnRequestedConnectHandler()
        => OnRequestedConnect?.Invoke();

    private void OnConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId, uint nextId)
        => OnConnected?.Invoke(context, playerId, nextId);

    private void OnRequestedDisconnectHandler()
        => OnRequestedDisconnect?.Invoke();
    
    private void OnDisconnectedHandler(IRelayClientNetworkThreadContext context)
        => OnDisconnected?.Invoke(context);
    
    private void OnOtherPlayerConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnOtherPlayerConnected?.Invoke(context, playerId);

    private void OnOtherPlayerDisconnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnOtherPlayerDisconnected?.Invoke(context, playerId);

    private void OnRequestedJoinAreaHandler(AreaId areaId)
        => OnRequestedJoinArea?.Invoke(areaId);

    private void OnRequestedSetActiveCellsEvent(ReadOnlyArray<CellId> cellIds)
        => OnRequestedSetActiveCells?.Invoke(cellIds);

    private void OnJoinedAreaHandler(IRelayClientNetworkThreadContext context, AreaId areaId)
        => OnJoinedArea?.Invoke(context, areaId);

    private void OnRequestedLeaveAreaHandler()
        => OnRequestedLeaveArea?.Invoke();

    private void OnLeftAreaHandler(IRelayClientNetworkThreadContext context)
        => OnLeftArea?.Invoke(context);

    private void OnActiveCellsSetHandler(IRelayClientNetworkThreadContext context)
        => OnActiveCellsSet?.Invoke(context);

    private void OnOtherPlayerJoinedAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnOtherPlayerJoinedArea?.Invoke(context, playerId);

    private void OnOtherPlayerLeftAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnOtherPlayerLeftArea?.Invoke(context, playerId);
    
    private void OnPingUpdatedHandler(int ping)
        => OnPingUpdated?.Invoke(ping);
    
    private void OnAnyBuiltInMessageHandler(ServerEventHeader header, NetDataReader reader)
    {
        var serverHandler = _serverMessageHandlers[(int)header.EventCode];
        
        if (serverHandler != null)
        {
            var position = reader.Position;
            foreach (var handlerUntyped in serverHandler.GetInvocationList())
            {
                reader.SetPosition(position);
                var handler = (Action<ServerEventHeader, NetDataReader>) handlerUntyped;
                handler.Invoke(header, reader);
            }
        }
    }
    
    private void OnAnyServerRpcMessageHandler(ServerEventHeader header, NetDataReader reader)
    {
        var serverHandler = _serverMessageHandlers[(int)header.EventCode];
        
        if (serverHandler != null)
        {
            var position = reader.Position;
            foreach (var handlerUntyped in serverHandler.GetInvocationList())
            {
                reader.SetPosition(position);
                var handler = (Action<ServerEventHeader, NetDataReader>) handlerUntyped;
                handler.Invoke(header, reader);
            }
        }
    }
    
    private void OnAnyClientRpcMessageHandler(CustomRelayEventHeader header, NetDataReader reader)
    {
        var clientHandler = _clientMessageHandlers[(int)header.EventCode];
        
        if (clientHandler != null)
        {
            var position = reader.Position;
            foreach (var handlerUntyped in clientHandler.GetInvocationList())
            {
                reader.SetPosition(position);
                var handler = (Action<CustomRelayEventHeader, NetDataReader>) handlerUntyped;
                handler.Invoke(header, reader);
            }
        }
    }
    
    private void OnClientUpdateHandler(IRelayClientNetworkThreadContext context)
        => OnClientUpdate?.Invoke(context);

    #endregion
}
