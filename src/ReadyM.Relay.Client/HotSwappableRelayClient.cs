using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        client.OnRequestedSetActiveCells += OnRequestedSetActiveCellsHandler;
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
        client.OnRequestedSetActiveCells -= OnRequestedSetActiveCellsHandler;
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

    public ReadOnlyList<CellId>? RequestedActiveCells => _client?.RequestedActiveCells;

    public event Action? OnStart;
    public event Action? OnRequestedStop;

    public event Action? OnRequestedConnect;
    public event Action<IRelayClientNetworkThreadContext, PlayerId, uint>? OnConnected;
    public event Action? OnRequestedDisconnect;
    public event Action<IRelayClientNetworkThreadContext>? OnDisconnected;
    
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerConnected;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerDisconnected;
    public event Action<AreaId>? OnRequestedJoinArea;
    public event Action<ReadOnlyList<CellId>>? OnRequestedSetActiveCells;
    public event Action<IRelayClientNetworkThreadContext, AreaId>? OnJoinedArea;
    public event Action? OnRequestedLeaveArea;
    public event Action<IRelayClientNetworkThreadContext>? OnLeftArea;
    public event Action<IRelayClientNetworkThreadContext>? OnActiveCellsSet;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerJoinedArea;
    public event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerLeftArea;
    public event Action<int>? OnPingUpdated;

    public int PacketLossPercent => _client?.PacketLossPercent ?? 0;
    
    private readonly RelayMessageHandlerRegistry _handlers = new();

    public event Action<ServerEventHeader, NetDataReader>? OnAnyBuiltInMessage
    {
        add => _handlers.AnyBuiltIn += value;
        remove => _handlers.AnyBuiltIn -= value;
    }

    public event Action<ServerEventHeader, NetDataReader>? OnAnyServerRpcMessage
    {
        add => _handlers.AnyServerRpc += value;
        remove => _handlers.AnyServerRpc -= value;
    }

    public event Action<CustomRelayEventHeader, NetDataReader>? OnAnyClientRpcMessage
    {
        add => _handlers.AnyClientRpc += value;
        remove => _handlers.AnyClientRpc -= value;
    }

    public void AddBuiltInMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
        => _handlers.AddBuiltIn(eventCode, eventCode, handler);

    public void AddBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler)
        => _handlers.AddBuiltIn(minEventCode, maxEventCode, handler);

    public void RemoveBuiltInMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
        => _handlers.RemoveBuiltIn(eventCode, eventCode, handler);

    public void RemoveBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler)
        => _handlers.RemoveBuiltIn(minEventCode, maxEventCode, handler);

    public void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
        => _handlers.AddServerRpc(eventCode, eventCode, handler);

    public void AddServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler)
        => _handlers.AddServerRpc(minEventCode, maxEventCode, handler);

    public void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
        => _handlers.RemoveServerRpc(eventCode, eventCode, handler);

    public void RemoveServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler)
        => _handlers.RemoveServerRpc(minEventCode, maxEventCode, handler);

    public void AddClientRpcMessageHandler(RelayMessageCode eventCode, Action<CustomRelayEventHeader, NetDataReader> handler)
        => _handlers.AddClientRpc(eventCode, eventCode, handler);

    public void AddClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<CustomRelayEventHeader, NetDataReader> handler)
        => _handlers.AddClientRpc(minEventCode, maxEventCode, handler);

    public void RemoveClientRpcMessageHandler(RelayMessageCode eventCode, Action<CustomRelayEventHeader, NetDataReader> handler)
        => _handlers.RemoveClientRpc(eventCode, eventCode, handler);

    public void RemoveClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<CustomRelayEventHeader, NetDataReader> handler)
        => _handlers.RemoveClientRpc(minEventCode, maxEventCode, handler);

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

    public void BeginExpectedStall()
        => _client!.BeginExpectedStall();

    public void EndExpectedStall()
        => _client!.EndExpectedStall();

    public void RequestJoinArea(AreaId areaId)
        => _client!.RequestJoinArea(areaId);

    public void RequestSetActiveCells(IEnumerable<CellId> cellIds)
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

    private void OnRequestedSetActiveCellsHandler(ReadOnlyList<CellId> cellIds)
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
        => _handlers.InvokeServerMessage(header, reader);

    private void OnAnyServerRpcMessageHandler(ServerEventHeader header, NetDataReader reader)
        => _handlers.InvokeServerMessage(header, reader);

    private void OnAnyClientRpcMessageHandler(CustomRelayEventHeader header, NetDataReader reader)
        => _handlers.InvokeClientMessage(header, reader);

    private void OnClientUpdateHandler(IRelayClientNetworkThreadContext context)
        => OnClientUpdate?.Invoke(context);

    #endregion
}
