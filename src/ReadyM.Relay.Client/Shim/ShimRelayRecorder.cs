using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
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

    public Action? OnRecordingStarted;
    public Action? OnRecordingStopped;
    
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

        if (relayClient.IsRunning)
            throw new InvalidOperationException("Relay client is already running. Please stop it before attaching to a recorder.");
        
        logger.LogDebug("Attaching shim relay client for recording");
        
        _relayClient = relayClient;
        _relayClient.OnPeerIdAssigned += OnPeerIdAssigned;
        _relayClient.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
        _relayClient.OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
        _relayClient.OnPlayerPropertiesAdded += OnPlayerPropertiesAdded;
        _relayClient.OnCustomEvent += OnCustomEvent;
        _relayClient.OnAfterJoinedRoom += OnAfterJoinedRoom;
        _relayClient.OnOtherPlayerJoined += OnOtherPlayerJoined;
        _relayClient.OnOtherPlayerLeft += OnOtherPlayerLeft;
        _relayClient.OnDisconnected += OnDisconnected;
        _relayClient.OnPingUpdated += OnPingUpdated;
        _relayClient.OnEcsDelta += OnEcsDelta;
        _relayClient.OnReceivedDeleteEntity += OnReceivedDeleteEntity;
        _relayClient.OnBlobAck += OnBlobAck;
        _relayClient.OnBlobData += OnBlobData;
    }

    public void Detach()
    {
        if (_isRecording)
            throw new InvalidOperationException("Cannot detach relay client while recording is in progress.");
        if (_relayClient == null)
            throw new InvalidOperationException("Relay client is not attached.");
        
        logger.LogDebug("Detaching shim relay client from recording");

        _relayClient.OnBlobAck -= OnBlobAck;
        _relayClient.OnBlobData -= OnBlobData;
        _relayClient.OnReceivedDeleteEntity -= OnReceivedDeleteEntity;
        _relayClient.OnEcsDelta -= OnEcsDelta;
        _relayClient.OnPingUpdated -= OnPingUpdated;
        _relayClient.OnDisconnected -= OnDisconnected;
        _relayClient.OnOtherPlayerJoined -= OnOtherPlayerJoined;
        _relayClient.OnAfterJoinedRoom -= OnAfterJoinedRoom;
        _relayClient.OnCustomEvent -= OnCustomEvent;
        _relayClient.OnPlayerPropertiesAdded -= OnPlayerPropertiesAdded;
        _relayClient.OnPlayerPropertiesChanged -= OnPlayerPropertiesChanged;
        _relayClient.OnRoomPropertiesChanged -= OnRoomPropertiesChanged;
        _relayClient.OnPeerIdAssigned -= OnPeerIdAssigned;
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
    
    private void OnPeerIdAssigned(PlayerId playerId, Dictionary<object, object> initialState)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.PeerIdAssigned,
            PlayerId = playerId,
            InitialState = new ShimInitialState(initialState),
        };
        AddItem(item);
    }
    
    private void OnRoomPropertiesChanged(Dictionary<object, object?> changes)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.RoomPropertiesChanged,
            Changes = new ShimChanges(changes),
        };
        AddItem(item);
    }
    
    private void OnPlayerPropertiesChanged(PlayerId playerId, Dictionary<object, object?> changes)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.PlayerPropertiesChanged,
            PlayerId = playerId,
            Changes = new ShimChanges(changes),
        };
        AddItem(item);
    }

    private void OnPlayerPropertiesAdded(PlayerId playerId, Dictionary<object, object?> changes)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.PlayerPropertiesAdded,
            PlayerId = playerId,
            Changes = new ShimChanges(changes),
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
    
    private void OnCustomEvent(CustomEventHeader ev, NetDataReader reader)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.CustomEvent,
            EventHeader = ev,
            RawData = GetShimBuffer(reader),
        };
        AddItem(item);
    }

    private void OnReceivedDeleteEntity(NetworkIdComponent networkId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.ReceivedDestroyEntity,
            NetworkId = networkId,
        };
        AddItem(item);
    }

    private void OnEcsDelta(NetDataReader reader)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.EcsDelta,
            RawData = GetShimBuffer(reader),
        };
        AddItem(item);
    }

    private void OnPingUpdated(int ping)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.PingUpdated,
            Ping = ping,
        };
        AddItem(item);
    }

    private void OnDisconnected(DisconnectReason reason)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.Disconnected,
            DisconnectReason = reason,
        };
        AddItem(item);
    }

    private void OnAfterJoinedRoom(Dictionary<object, object> initialState)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.JoinedRoom,
            InitialState = new ShimInitialState(initialState),
        };
        AddItem(item);
    }

    private void OnOtherPlayerJoined(PlayerId playerId, Dictionary<object, object> initialState)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.OtherPlayerJoinedRoom,
            PlayerId = playerId,
            InitialState = new ShimInitialState(initialState),
        };
        AddItem(item);
    }
    
    private void OnOtherPlayerLeft(PlayerId playerId)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.OtherPlayerLeft,
            PlayerId = playerId,
        };
        AddItem(item);
    }

    private void OnBlobAck(int requestId, bool result)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.BlobAck,
            BlobRequestId = requestId,
            BlobAckResult = result,
        };
        AddItem(item);
    }

    private void OnBlobData(int requestId, BlobInfo? blobData)
    {
        var item = new ShimItem()
        {
            Kind = ShimItemKind.BlobData,
            BlobRequestId = requestId,
            BlobData = blobData,
        };
        AddItem(item);
    }
}
