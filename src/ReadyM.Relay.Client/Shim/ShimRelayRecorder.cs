using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Shim;

namespace ReadyM.Relay.Client.Shim;

internal class ShimRelayRecorder : IDisposable
{
    private readonly object _lock = new();
    
    private readonly List<ShimResponseItem> _responseItems = new();
    private readonly IRelayClient _attachedRelayClient;
    private bool _isRecording;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly ShimRelayMessageParser _parser;
    private readonly ILogger _logger;

    public IRelayClient AttachedRelayClient => _attachedRelayClient; 

    public event Action? OnRecordingStarted;
    public event Action? OnRecordingStopped;

    public ShimRelayRecorder(IRelayClient attachedRelayClient, ShimRelayMessageParser parser, ILogger logger)
    {
        _parser = parser;
        _logger = logger;
        _attachedRelayClient = attachedRelayClient;
        
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
    }

    public void Dispose()
    {
        if (_isRecording)
            StopRecording();
            
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
    }
    
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
        
        _logger.LogDebug("Starting shim recording");
        
        _stopwatch.Start();
        OnRecordingStarted?.Invoke();
    }

    public void StopRecording()
    {
        if (!_isRecording)
            return;
        
        _logger.LogDebug("Stopping shim recording");
        
        OnRecordingStopped?.Invoke();
        _isRecording = false;
        _stopwatch.Stop();
    }

    private void AddResponseItem(ShimResponseItem responseItem)
    {
        responseItem.Elapsed = _stopwatch.ElapsedMilliseconds;
        lock (_lock)
        {
            _responseItems.Add(responseItem);
        }
    }
    
    private void OnConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId, uint nextId)
    {
        var otherPlayers = context.AllPlayers.ToList();
        otherPlayers.Remove(playerId);
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.Connected,
            PlayerId = playerId,
            OtherPlayers = otherPlayers,
            NextId = nextId
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
        var playerId = context.PlayerId!.Value;
        var otherPlayers = context.AreaPlayers.ToList();
        otherPlayers.Remove(playerId);
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.JoinedArea,
            AreaId = areaId,
            PlayerId = context.PlayerId!.Value,
            OtherPlayers = otherPlayers,
        };
        AddResponseItem(responseItem);
    }

    private void OnLeftAreaHandler(IRelayClientNetworkThreadContext context)
    {
        var responseItem = new ShimResponseItem()
        {
            Kind = ShimResponseKind.LeftArea,
            PlayerId = context.PlayerId!.Value,
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
        var customData = _parser.GetBuiltInResponseCustomData(header, reader);
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
        var customData = _parser.GetServerRpcResponseCustomData(header, reader);
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
        var customData = _parser.GetClientRpcResponseCustomData(header, reader);
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
