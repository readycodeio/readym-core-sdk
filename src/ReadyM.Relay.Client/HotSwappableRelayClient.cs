using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Relay.Client;

public class HotSwappableRelayClient : IRelayClient
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
            if (_client.IsRunning)
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
            if (_client.IsRunning)
                throw new InvalidOperationException("Cannot swap RelayClient while it is connected. Please stop the client first.");
                
            OnRelayClientDetach?.Invoke(_client);
            DetachRelayClient(_client);
        }

        _client = null;
    }

    private void AttachRelayClient(IRelayClient client)
    {
        client.OnRequestedStart += OnRequestedStartHandler;
        client.OnRequestedStop += OnRequestedStopHandler;
        client.OnRequestedConnect += OnRequestedConnectHandler;
        client.OnConnected += OnConnectedHandler;
        client.OnRequestedDisconnect += OnRequestedDisconnectHandler;
        client.OnDisconnected += OnDisconnectedHandler;
        client.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        client.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        client.OnRequestedJoinArea += OnRequestedJoinAreaHandler;
        client.OnJoinedArea += OnJoinedAreaHandler;
        client.OnRequestedLeaveArea += OnRequestedLeaveAreaHandler;
        client.OnLeftArea += OnLeftAreaHandler;
        client.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        client.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        client.OnPingUpdated += OnPingUpdatedHandler;
        client.OnAnyMessage += OnAnyMessageHandler;
        client.OnClientUpdate += OnClientUpdateHandler;
    }

    private void DetachRelayClient(IRelayClient client)
    {
        client.OnClientUpdate -= OnClientUpdateHandler;
        client.OnAnyMessage += OnAnyMessageHandler;
        client.OnPingUpdated += OnPingUpdatedHandler;
        client.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        client.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        client.OnLeftArea += OnLeftAreaHandler;
        client.OnRequestedLeaveArea += OnRequestedLeaveAreaHandler;
        client.OnJoinedArea += OnJoinedAreaHandler;
        client.OnRequestedJoinArea += OnRequestedJoinAreaHandler;
        client.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        client.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        client.OnDisconnected += OnDisconnectedHandler;
        client.OnRequestedDisconnect += OnRequestedDisconnectHandler;
        client.OnConnected += OnConnectedHandler;
        client.OnRequestedConnect += OnRequestedConnectHandler;
        client.OnRequestedStop += OnRequestedStopHandler;
        client.OnRequestedStart += OnRequestedStartHandler;
    }

    public void Dispose()
    {
        // empty
    }

    public bool IsRunning
        => _client?.IsRunning ?? false;

    public PlayerId PlayerId
        => _client?.PlayerId ?? PlayerId.Invalid;

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

    public PendingActionScheduler<IRelayClientNetworkThreadContext> Scheduler
        => _client!.Scheduler;

    public int GetMaxPacketSize(DeliveryMethod deliveryMethod)
        => _client?.GetMaxPacketSize(deliveryMethod) ?? 1300;

    public Task StartAsync(CancellationToken token, bool autoConnect = true)
        => _client!.StartAsync(token, autoConnect);

    public Task RunAsync(CancellationToken token)
        => _client!.RunAsync(token);

    public void Stop()
        => _client!.Stop();

    public void Connect()
        => _client!.Connect();

    public void Disconnect()
        => _client!.Disconnect();

    public void Reconnect()
        => _client!.Reconnect();

    public void JoinArea(AreaId areaId)
        => _client!.JoinArea(areaId);

    public void LeaveArea()
        => _client!.LeaveArea();

    public void SendRawMessage(NetDataWriter writer, DeliveryMethod deliveryMethod)
        => _client!.SendRawMessage(writer, deliveryMethod);

    public void SendMessage(RelayMessage message)
        => _client!.SendMessage(message);

    public void SendMessageToServer<T>(RelayMessageCode eventCode, T data, DeliveryMethod deliveryMethod)
        where T : INetSerializable
        => _client!.SendMessageToServer(eventCode, data, deliveryMethod);

    public void SendMessageToPeers<T>(RelayMessageCode eventCode, T data, PlayerId[] peers, DeliveryMethod deliveryMethod)
        where T : INetSerializable
        => _client!.SendMessageToPeers(eventCode, data, peers, deliveryMethod);

    public void SendMessageRelayMode<T>(RelayMessageCode eventCode, T data, RelayMode mode, DeliveryMethod deliveryMethod)
        where T : INetSerializable
        => _client!.SendMessageRelayMode(eventCode, data, mode, deliveryMethod);

    #region Event handlers
    
    private void OnRequestedStartHandler()
        => OnRequestedStart?.Invoke();

    private void OnRequestedStopHandler()
        => OnRequestedStop?.Invoke();

    private void OnRequestedConnectHandler()
        => OnRequestedConnect?.Invoke();

    private void OnConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnConnected?.Invoke(context, playerId);

    private void OnRequestedDisconnectHandler()
        => OnRequestedDisconnect?.Invoke();
    
    private void OnDisconnectedHandler(IRelayClientNetworkThreadContext context, DisconnectReason disconnectReason)
        => OnDisconnected?.Invoke(context, disconnectReason);
    
    private void OnOtherPlayerConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnOtherPlayerConnected?.Invoke(context, playerId);

    private void OnOtherPlayerDisconnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnOtherPlayerDisconnected?.Invoke(context, playerId);

    private void OnRequestedJoinAreaHandler(AreaId areaId)
        => OnRequestedJoinArea?.Invoke(areaId);

    private void OnJoinedAreaHandler(IRelayClientNetworkThreadContext context, AreaId areaId)
        => OnJoinedArea?.Invoke(context, areaId);

    private void OnRequestedLeaveAreaHandler()
        => OnRequestedLeaveArea?.Invoke();

    private void OnLeftAreaHandler(IRelayClientNetworkThreadContext context)
        => OnLeftArea?.Invoke(context);

    private void OnOtherPlayerJoinedAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnOtherPlayerJoinedArea?.Invoke(context, playerId);

    private void OnOtherPlayerLeftAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
        => OnOtherPlayerLeftArea?.Invoke(context, playerId);
    
    private void OnPingUpdatedHandler(IRelayClientNetworkThreadContext context, int ping)
        => OnPingUpdated?.Invoke(context, ping);
    
    private void OnAnyMessageHandler(IRelayClientNetworkThreadContext context, CustomEventHeader header, NetDataReader reader)
    {
        var handler = _messageHandlers[header.EventCode];
        handler?.Invoke(context, header, reader);
    }
    
    private void OnClientUpdateHandler(IRelayClientNetworkThreadContext context)
        => OnClientUpdate?.Invoke(context);

    #endregion
}
