using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Client.Blobs;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Common.Shim;

namespace ReadyM.Relay.Client.Shim;

public class ShimRelayRecorder(ShimRelayMessageParser parser, ILogger logger)
{
    private readonly object _lock = new();
    
    private readonly List<ShimResponseItem> _responseItems = new();
    private IShimRecordableRelayClient? _attachedRelayClient;
    private bool _isRecording;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    
    public bool IsAttached
        => _attachedRelayClient != null;

    public event Action<IRelayClient>? OnAttached;
    public event Action<IRelayClient>? OnDetached;
    public event Action? OnRecordingStarted;
    public event Action? OnRecordingStopped;
    
    public IShimRecordableRelayClient? AttachedRelayClient => _attachedRelayClient; 

    public ShimRecording GetRecording()
    {
        lock (_lock)
        {
            return new ShimRecording(_responseItems, _attachedRelayClient?.PlayerId);
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
        if (_attachedRelayClient != null)
            throw new InvalidOperationException("Relay client is already attached.");

        if (relayClient.RequestedConnect)
            throw new InvalidOperationException("Relay client is already running. Please stop it before attaching to a recorder.");
        
        logger.LogDebug("Attaching shim relay client for recording");
        
        _attachedRelayClient = relayClient;
        _attachedRelayClient.OnConnected += OnConnectedHandler;
        _attachedRelayClient.OnDisconnected += OnDisconnectedHandler;
        _attachedRelayClient.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        _attachedRelayClient.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        _attachedRelayClient.OnJoinedArea += OnJoinedAreaHandler;
        _attachedRelayClient.OnLeftArea += OnLeftAreaHandler;
        _attachedRelayClient.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        _attachedRelayClient.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        _attachedRelayClient.OnPingUpdated += OnPingUpdatedHandler;
        _attachedRelayClient.OnAnyBuiltInMessage += OnAnyBuiltInMessageHandler;
        _attachedRelayClient.OnAnyServerRpcMessage += OnAnyServerRpcMessageHandler;
        _attachedRelayClient.OnAnyClientRpcMessage += OnAnyClientRpcMessageHandler;

        OnAttached?.Invoke(_attachedRelayClient);
    }

    public void Detach()
    {
        if (_isRecording)
            throw new InvalidOperationException("Cannot detach relay client while recording is in progress.");
        if (_attachedRelayClient == null)
            throw new InvalidOperationException("Relay client is not attached.");
        
        logger.LogDebug("Detaching shim relay client from recording");

        OnDetached?.Invoke(_attachedRelayClient);
        
        _attachedRelayClient.OnAnyClientRpcMessage -= OnAnyClientRpcMessageHandler;
        _attachedRelayClient.OnAnyServerRpcMessage -= OnAnyServerRpcMessageHandler;
        _attachedRelayClient.OnAnyBuiltInMessage -= OnAnyBuiltInMessageHandler;
        _attachedRelayClient.OnPingUpdated -= OnPingUpdatedHandler;
        _attachedRelayClient.OnOtherPlayerLeftArea -= OnOtherPlayerLeftAreaHandler;
        _attachedRelayClient.OnOtherPlayerJoinedArea -= OnOtherPlayerJoinedAreaHandler;
        _attachedRelayClient.OnLeftArea -= OnLeftAreaHandler;
        _attachedRelayClient.OnJoinedArea -= OnJoinedAreaHandler;
        _attachedRelayClient.OnOtherPlayerDisconnected -= OnOtherPlayerDisconnectedHandler;
        _attachedRelayClient.OnOtherPlayerConnected -= OnOtherPlayerConnectedHandler;
        _attachedRelayClient.OnDisconnected -= OnDisconnectedHandler;
        _attachedRelayClient.OnConnected -= OnConnectedHandler;
        _attachedRelayClient = null;
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
