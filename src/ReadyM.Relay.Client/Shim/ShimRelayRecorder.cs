using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Common.Shim;

namespace ReadyM.Relay.Client.Shim;

public class ShimRelayRecorder(ShimRelayMessageParser parser, ILogger logger)
{
    private readonly object _lock = new();
    
    private readonly List<ShimResponseItem> _responseItems = new();
    private IShimRecordableRelayClient? _relayClient;
    private bool _isRecording;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    
    public bool IsAttached
        => _relayClient != null;

    public event Action? OnRecordingStarted;
    public event Action? OnRecordingStopped;
    
    public IShimRecordableRelayClient? RelayClient => _relayClient; 

    public ShimRecording GetRecording()
    {
        lock (_lock)
        {
            return new ShimRecording(_responseItems, _relayClient?.PlayerId);
        }
    }

    public void StartRecording()
    {
        if (_isRecording)
            return;
        _isRecording = true;
        
        logger.LogDebug("Starting shim recording");
        
        _stopwatch.Start();
        OnRecordingStarted?.Invoke();
    }

    public void StopRecording()
    {
        if (!_isRecording)
            return;
        
        logger.LogDebug("Stopping shim recording");
        
        OnRecordingStopped?.Invoke();
        _isRecording = false;
        _stopwatch.Stop();
    }
    
    public void Attach(IShimRecordableRelayClient relayClient)
    {
        if (_isRecording)
            throw new InvalidOperationException("Cannot attach relay client while recording is in progress.");
        if (_relayClient != null)
            throw new InvalidOperationException("Relay client is already attached.");

        if (relayClient.RequestedConnect)
            throw new InvalidOperationException("Relay client is already running. Please stop it before attaching to a recorder.");
        
        logger.LogDebug("Attaching shim relay client for recording");
        
        _relayClient = relayClient;
        _relayClient.OnConnected += OnConnectedHandler;
        _relayClient.OnDisconnected += OnDisconnectedHandler;
        _relayClient.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        _relayClient.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        _relayClient.OnJoinedArea += OnJoinedAreaHandler;
        _relayClient.OnLeftArea += OnLeftAreaHandler;
        _relayClient.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        _relayClient.OnPingUpdated += OnPingUpdatedHandler;
        _relayClient.OnAnyBuiltInMessage += OnAnyBuiltInMessageHandler;
        _relayClient.OnAnyServerRpcMessage += OnAnyServerRpcMessageHandler;
        _relayClient.OnAnyClientRpcMessage += OnAnyClientRpcMessageHandler;
    }

    public void Detach()
    {
        if (_isRecording)
            throw new InvalidOperationException("Cannot detach relay client while recording is in progress.");
        if (_relayClient == null)
            throw new InvalidOperationException("Relay client is not attached.");
        
        logger.LogDebug("Detaching shim relay client from recording");

        _relayClient.OnAnyClientRpcMessage -= OnAnyClientRpcMessageHandler;
        _relayClient.OnAnyServerRpcMessage -= OnAnyServerRpcMessageHandler;
        _relayClient.OnAnyBuiltInMessage -= OnAnyBuiltInMessageHandler;
        _relayClient.OnPingUpdated -= OnPingUpdatedHandler;
        _relayClient.OnOtherPlayerLeftArea -= OnOtherPlayerLeftAreaHandler;
        _relayClient.OnOtherPlayerJoinedArea -= OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnLeftArea -= OnLeftAreaHandler;
        _relayClient.OnJoinedArea -= OnJoinedAreaHandler;
        _relayClient.OnOtherPlayerDisconnected -= OnOtherPlayerDisconnectedHandler;
        _relayClient.OnOtherPlayerConnected -= OnOtherPlayerConnectedHandler;
        _relayClient.OnDisconnected -= OnDisconnectedHandler;
        _relayClient.OnConnected -= OnConnectedHandler;
        
        _relayClient = null;
    }

    private void AddResponseItem(ShimResponseItem responseItem)
    {
        responseItem.Elapsed = _stopwatch.ElapsedMilliseconds;
        lock (_lock)
        {
            _responseItems.Add(responseItem);
        }
    }
    
    private void OnConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.Connected,
            PlayerId = playerId,
        };
        AddResponseItem(responseItem);
    }

    private void OnDisconnectedHandler(IRelayClientNetworkThreadContext context, DisconnectReason disconnectReason)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.Disconnected,
            DisconnectReason = disconnectReason,
        };
        AddResponseItem(responseItem);
    }

    private void OnOtherPlayerConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.OtherPlayerConnected,
            PlayerId = playerId,
        };
        AddResponseItem(responseItem);
    }

    private void OnOtherPlayerDisconnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.OtherPlayerDisconnected,
            PlayerId = playerId,
        };
        AddResponseItem(responseItem);
    }

    private void OnJoinedAreaHandler(IRelayClientNetworkThreadContext context, AreaId areaId)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.JoinedArea,
            AreaId = areaId,
        };
        AddResponseItem(responseItem);
    }

    private void OnLeftAreaHandler(IRelayClientNetworkThreadContext context)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.LeftArea,
        };
        AddResponseItem(responseItem);
    }

    private void OnOtherPlayerJoinedAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.OtherPlayerJoinedArea,
            PlayerId = playerId,
        };
        AddResponseItem(responseItem);
    }

    private void OnOtherPlayerLeftAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.OtherPlayerLeftArea,
            PlayerId = playerId,
        };
        AddResponseItem(responseItem);
    }

    private void OnPingUpdatedHandler(IRelayClientNetworkThreadContext context, int ping)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.PingUpdated,
            Ping = ping,
        };
        AddResponseItem(responseItem);
    }

    private void OnAnyBuiltInMessageHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        var customData = parser.GetBuiltInResponseCustomData(header, reader);
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.AnyBuiltInMessage,
            ServerHeader = header,
            RawData = GetShimBuffer(reader),
            CustomData = customData,
        };
        AddResponseItem(responseItem);
    }

    private void OnAnyServerRpcMessageHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        var customData = parser.GetServerRpcResponseCustomData(header, reader);
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.AnyServerMessage,
            ServerHeader = header,
            RawData = GetShimBuffer(reader),
            CustomData = customData,
        };
        AddResponseItem(responseItem);
    }

    private void OnAnyClientRpcMessageHandler(IRelayClientNetworkThreadContext context, CustomRelayEventHeader header, NetDataReader reader)
    {
        var customData = parser.GetClientRpcResponseCustomData(header, reader);
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.AnyClientMessage,
            ClientHeader = header,
            RawData = GetShimBuffer(reader),
            CustomData = customData,
        };
        AddResponseItem(responseItem);
    }

    private ShimBuffer GetShimBuffer(NetDataReader reader)
    {
        if (reader.AvailableBytes > 0)
        {
            var newRawData = new byte[reader.AvailableBytes];
            Array.Copy(reader.RawData, reader.Position, newRawData, 0, reader.AvailableBytes);
            return new ShimBuffer(newRawData);
        }
        else
        {
            return new ShimBuffer([]);
        }
    }
}
