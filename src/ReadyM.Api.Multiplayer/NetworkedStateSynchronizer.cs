using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Systems;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

// There's some confusion as to the split of responsibilities between the RelayClient and the rest of the code base
// (formerly ReadyMultiplayerMod and its subclasses; and now NetworkedStateSynchronizer). We have to decide what each
// class is responsible for. For example, RelayClient doesn't call SendInitialPlayerState() on itself when the room
// is joined. But it does call other sync protocol-related methods.
// 
// Proposal: RelayClient should be an event-bus with minimal internal logic.
// NetworkedSynchronizer should be the part that subscribes to various events from multiple places and holistically
// managed synchronization by triggering events and responding to events.
public abstract class NetworkedStateSynchronizer(
    Store world,
    NetworkedEntityManager netManager,
    INetworkedComponentRegistry netComponentRegistry,
    RelayClient relayClient, 
    SystemUpdateLoop updateLoop,
    ILogger logger) : IDisposable
{
    private class RegisterJobsForNetworkedComponent(NetworkedStateSynchronizer owner) : INetworkedComponentRegistryCallback
    {
        private byte _nextComponentId;

        public void AcceptNetworkedComponent<T>()
            where T : struct, INetworkedComponent
        {
            var id = new NetworkedComponentId(_nextComponentId++);

            owner.Logger.LogDebug("Registering jobs for networked component: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner.World.SystemRoot.Add(new SendClientComponentDeltaSystem<T>(id, owner.RelayClient));
            owner.DeltaReaderJobs.Add(id, new ApplyDeltaJob<T>(owner.NetManager, () => owner.RelayClient.PlayerId));
            owner.SnapshotReaderJobs.Add(id, new ApplySnapshotJob<T>(owner.NetManager));
        }
    }

    private readonly ILogger Logger = logger;
    public Store World { get; } = world;
    public NetworkedEntityManager NetManager { get; } = netManager;
    public RelayClient RelayClient { get; } = relayClient;
    
    public bool IsRunning { get; private set; }
    
    internal readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> SnapshotReaderJobs = [];
    internal readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> DeltaReaderJobs = [];
    
    public event Action? OnBeforeJoinedRoom;
    public event Action? OnAfterJoinedRoom;
    public event Action<PlayerId>? OnOtherPlayerJoined;
    public event Action<PlayerId>? OnOtherPlayerLeft;
    public event Action<PlayerId, Dictionary<object, object?>>? OnPlayerPropertiesChanged;
    
    public virtual void Dispose()
    {
        if (IsRunning)
            Stop();
    }
    
    public void Start()
    {
        if (IsRunning)
            throw new InvalidOperationException("NetworkedStateSynchronizer is already started.");
        IsRunning = true;

        NetManager.OnEntityDeleted += HandleEntityDeleted;

        RelayClient.OnBeforeJoinedRoom += OnBeforeJoinedRoomHandler;
        RelayClient.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
        RelayClient.OnOtherPlayerJoined += OnOtherPlayerJoinedHandler;
        RelayClient.OnOtherPlayerLeft += OnOtherPlayerLeftHandler;
        RelayClient.OnPlayerPropertiesChanged += OnPlayerPropertiesChangedHandler;
        RelayClient.OnEnterRoomRequest += OnEnterRoomRequest;
        RelayClient.OnExitRoomRequest += OnExitRoomRequest;

        RelayClient.OnEcsDelta += OnEcsDelta;
        RelayClient.OnEcsSnapshot += OnEcsSnapshot;
        RelayClient.OnReceivedDeleteEntity += DeleteRemoteEntityFromEcs;
        
        // NOTE: iterate over all components with generics without reflection
        netComponentRegistry.Accept(new RegisterJobsForNetworkedComponent(this));
    }

    public void Stop()
    {
        if (!IsRunning)
            throw new InvalidOperationException("NetworkedStateSynchronizer is not started.");
        IsRunning = false;
        
        RelayClient.OnReceivedDeleteEntity -= DeleteRemoteEntityFromEcs;
        RelayClient.OnEcsSnapshot -= OnEcsSnapshot;
        RelayClient.OnEcsDelta -= OnEcsDelta;
        
        RelayClient.OnExitRoomRequest -= OnExitRoomRequest;
        RelayClient.OnEnterRoomRequest -= OnEnterRoomRequest;
        RelayClient.OnPlayerPropertiesChanged -= OnPlayerPropertiesChangedHandler;
        RelayClient.OnOtherPlayerLeft -= OnOtherPlayerLeftHandler;
        RelayClient.OnOtherPlayerJoined -= OnOtherPlayerJoinedHandler;
        RelayClient.OnAfterJoinedRoom -= OnAfterJoinedRoomHandler;
        RelayClient.OnBeforeJoinedRoom -= OnBeforeJoinedRoomHandler;
        
        NetManager.OnEntityDeleted -= HandleEntityDeleted;
        
        // FIXME: World.RootSystem not being cleaned up
        SnapshotReaderJobs.Clear();
        DeltaReaderJobs.Clear();
    }
    
    #region Event handlers

    private void OnEcsDelta(NetDataReader reader)
    {
        var componentId = reader.Get<NetworkedComponentId>();

        if (!DeltaReaderJobs.TryGetValue(componentId, out var readerJob) || readerJob == null)
        {
            Logger.LogError("No delta reader job registered for component ID: {Id}", componentId);
            return;
        }

        var bytesToCopy = reader.GetRemainingBytes();
        var readerCopy = new NetDataReader(bytesToCopy, 0, bytesToCopy.Length);

        RunOnGameThread(() =>
        {
            readerJob.Execute(readerCopy);
        });
    }

    private void OnEcsSnapshot(NetDataReader reader)
    {
        var bytesToCopy = reader.GetRemainingBytes();
        var readerCopy = new NetDataReader(bytesToCopy, 0, bytesToCopy.Length);

        RunOnGameThread(() =>
        {
            while (readerCopy.TryGetNetworkedComponentId(out var componentId))
            {
                if (!SnapshotReaderJobs.TryGetValue(componentId, out var readerJob) || readerJob == null)
                {
                    Logger.LogError("No snapshot reader job registered for component ID: {Id}", componentId);
                    return;
                }

                readerJob.Execute(readerCopy);
            }
        });
    }
    
    protected virtual void OnBeforeJoinedRoomHandler()
    {
        OnBeforeJoinedRoom?.Invoke();
    }
    
    protected virtual void OnAfterJoinedRoomHandler()
    {
        OnAfterJoinedRoom?.Invoke();
    }

    protected virtual void OnOtherPlayerJoinedHandler(PlayerId playerId)
    {
        OnOtherPlayerJoined?.Invoke(playerId);
    }

    protected virtual void OnOtherPlayerLeftHandler(PlayerId playerId)
    {
        OnOtherPlayerLeft?.Invoke(playerId);
    }
    
    protected virtual void OnPlayerPropertiesChangedHandler(PlayerId playerId, Dictionary<object, object?> changes)
    {
        OnPlayerPropertiesChanged?.Invoke(playerId, changes);
    }
    
    protected virtual void OnEnterRoomRequest()
    {
        // empty
    }

    protected virtual void OnExitRoomRequest()
    {
        // empty
    }
    
    #endregion
    
    #region ECS

    private void HandleEntityDeleted(NetworkIdComponent netId)
    {
        if (netId.Creator == RelayClient.LocalPlayer.PlayerId)
        {
            // our own entity - send destroy event
            Logger.LogDebug("Networked entity destroyed: {Id} (owned)", netId);
            var writer = new NetDataWriter();
            writer.Put((byte)SystemEvent.DestroyEntity);
            writer.Put(netId);
            RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.ReliableOrdered); // TODO: Use RPC API instead
        }
    }

    private void DeleteRemoteEntityFromEcs(NetworkIdComponent netId)
    {
        if (NetManager.TryGetEntityByNetworkId(netId, out var entity))
        {
            Logger.LogDebug("Queueing remote entity for destruction: {Id}", netId);
            updateLoop.CommandBuffer.DeleteEntity(entity.Value.Id);
        }
        else
        {
            Logger.LogError("Received destroy event for locally non-existent entity: {Id}", netId);
        }
    }

    #endregion
    
    protected abstract void RunOnGameThread(Action action);
}
