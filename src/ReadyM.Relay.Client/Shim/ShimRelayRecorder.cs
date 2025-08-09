using System;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Common.Shim;

namespace ReadyM.Relay.Client.Shim;

public class ShimRelayRecorder(ILogger logger)
{
    private ShimRecording? _recording;
    private IShimRecordableRelayClient? _relayClient;
    private bool _isRecording;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    
    public bool IsAttached
        => _relayClient != null;

    public event Action? OnRecordingStarted;
    public event Action? OnRecordingStopped;
    
    public IShimRecordableRelayClient? RelayClient => _relayClient; 
    
    public void SetRecording(ShimRecording recording)
    {
        if (_isRecording)
            throw new InvalidOperationException("Cannot set recording while already recording.");
        if (_recording != null)
            throw new InvalidOperationException("Recording is already set.");

        _recording = recording;
    }

    public ShimRecording? GetRecording()
    {
        if (_recording == null)
            return null;
        
        lock (_recording)
        {
            return new ShimRecording(_recording);
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
        _relayClient.OnRequestedConnect += OnRequestedConnectHandler;
        _relayClient.OnConnected += OnConnectedHandler;
        _relayClient.OnRequestedDisconnect += OnRequestedDisconnectHandler;
        _relayClient.OnDisconnected += OnDisconnectedHandler;
        _relayClient.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        _relayClient.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        _relayClient.OnRequestedJoinArea += OnRequestedJoinAreaHandler;
        _relayClient.OnJoinedArea += OnJoinedAreaHandler;
        _relayClient.OnRequestedLeaveArea += OnRequestedLeaveAreaHandler;
        _relayClient.OnLeftArea += OnLeftAreaHandler;
        _relayClient.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        _relayClient.OnPingUpdated += OnPingUpdatedHandler;
        _relayClient.OnAnyServerRpcMessage += OnAnyServerMessageHandler;
        _relayClient.OnAnyClientRpcMessage += OnAnyClientMessageHandler;
    }

    public void Detach()
    {
        if (_isRecording)
            throw new InvalidOperationException("Cannot detach relay client while recording is in progress.");
        if (_relayClient == null)
            throw new InvalidOperationException("Relay client is not attached.");
        
        logger.LogDebug("Detaching shim relay client from recording");

        _relayClient.OnAnyClientRpcMessage -= OnAnyClientMessageHandler;
        _relayClient.OnAnyServerRpcMessage -= OnAnyServerMessageHandler;
        _relayClient.OnPingUpdated -= OnPingUpdatedHandler;
        _relayClient.OnOtherPlayerLeftArea -= OnOtherPlayerLeftAreaHandler;
        _relayClient.OnOtherPlayerJoinedArea -= OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnLeftArea -= OnLeftAreaHandler;
        _relayClient.OnRequestedLeaveArea -= OnRequestedLeaveAreaHandler;
        _relayClient.OnJoinedArea -= OnJoinedAreaHandler;
        _relayClient.OnRequestedJoinArea -= OnRequestedJoinAreaHandler;
        _relayClient.OnOtherPlayerDisconnected -= OnOtherPlayerDisconnectedHandler;
        _relayClient.OnOtherPlayerConnected -= OnOtherPlayerConnectedHandler;
        _relayClient.OnDisconnected -= OnDisconnectedHandler;
        _relayClient.OnRequestedDisconnect -= OnRequestedDisconnectHandler;
        _relayClient.OnConnected -= OnConnectedHandler;
        _relayClient.OnRequestedConnect -= OnRequestedConnectHandler;
        
        _relayClient = null;
    }

    private void AddItem(ShimItem item)
    {
        item.Elapsed = _stopwatch.ElapsedMilliseconds;
        lock (_recording!)
        {
            _recording.AddItem(item);
        }
    }

    private void OnRequestedConnectHandler()
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.RequestedConnect,
        };
        AddItem(item);
    }
    
    private void OnConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.Connected,
            PlayerId = playerId,
        };
        AddItem(item);
    }

    private void OnRequestedDisconnectHandler()
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.RequestedDisconnect,
        };
        AddItem(item);
    }

    private void OnDisconnectedHandler(IRelayClientNetworkThreadContext context, DisconnectReason disconnectReason)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.Disconnected,
            DisconnectReason = disconnectReason,
        };
        AddItem(item);
    }

    private void OnOtherPlayerConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.OtherPlayerConnected,
            PlayerId = playerId,
        };
        AddItem(item);
    }

    private void OnOtherPlayerDisconnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.OtherPlayerDisconnected,
            PlayerId = playerId,
        };
        AddItem(item);
    }

    private void OnRequestedJoinAreaHandler(AreaId areaId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.RequestedJoinArea,
            AreaId = areaId,
        };
        AddItem(item);
    }

    private void OnJoinedAreaHandler(IRelayClientNetworkThreadContext context, AreaId areaId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.JoinedArea,
            AreaId = areaId,
        };
        AddItem(item);
    }

    private void OnRequestedLeaveAreaHandler()
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.RequestedLeaveArea,
        };
        AddItem(item);
    }

    private void OnLeftAreaHandler(IRelayClientNetworkThreadContext context)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.LeftArea,
        };
        AddItem(item);
    }

    private void OnOtherPlayerJoinedAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.OtherPlayerJoinedArea,
            PlayerId = playerId,
        };
        AddItem(item);
    }

    private void OnOtherPlayerLeftAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.OtherPlayerLeftArea,
            PlayerId = playerId,
        };
        AddItem(item);
    }

    private void OnPingUpdatedHandler(IRelayClientNetworkThreadContext context, int ping)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.PingUpdated,
            Ping = ping,
        };
        AddItem(item);
    }

    private void OnAnyServerMessageHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.AnyServerMessage,
            ServerHeader = header,
            RawData = GetShimBuffer(reader),
        };
        AddItem(item);
    }

    private void OnAnyClientMessageHandler(IRelayClientNetworkThreadContext context, CustomRelayEventHeader header, NetDataReader reader)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.AnyClientMessage,
            ClientHeader = header,
            RawData = GetShimBuffer(reader),
        };
        AddItem(item);
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
