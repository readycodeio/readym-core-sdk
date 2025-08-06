using System;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Systems;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Relay.Client;

public class ClientNetworkedStateSynchronizer(
    Store world,
    NetworkedEntityManager netEntity,
    ClientJobRegistry jobRegistry,
    INetworkedComponentRegistry netComponentRegistry,
    IRelayClient relayClient, 
    IClientEcsUpdateLoop ecsLoop,
    ILogger logger) : IDisposable
{
    protected IClientEcsUpdateLoop EcsLoop => ecsLoop;
    
    private class RegisterJobRegistryCallback(ClientNetworkedStateSynchronizer owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptComponent<T>(INetworkedComponentRegistry registry)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();

            owner.Logger.LogDebug("Registering jobs for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner.JobRegistry.DeltaReaderJobs.Add(id, new ApplyDeltaJob<T>(owner.NetEntity, () => owner.RelayClient.PlayerId));
            owner.JobRegistry.SnapshotReaderJobs.Add(id, new ApplySnapshotJob<T>(owner.NetEntity));
        }
    }
    
    private class RegisterSystemCallback(ClientNetworkedStateSynchronizer owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptComponent<T>(INetworkedComponentRegistry registry)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();
            
            owner.Logger.LogDebug("Registering client send for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner.World.SystemRoot.Add(new ClientSendComponentDeltaSystem<T>(id, owner.RelayClient));
        }
    }

    protected readonly Store World = world;
    protected readonly NetworkedEntityManager NetEntity = netEntity;
    protected readonly IRelayClient RelayClient = relayClient;
    protected readonly ILogger Logger = logger;
    
    protected readonly ClientJobRegistry JobRegistry = jobRegistry;
    
    public bool IsRunning { get; private set; }
    
    public event Action? OnJoinedArea;
    public event Action? OnLateJoinedArea;
    public event Action? OnLeftArea;
    public event Action<PlayerId>? OnOtherPlayerJoinedArea;
    public event Action<PlayerId>? OnOtherPlayerLeftArea;

    public event Action? OnEcsSnapshot;
    
    public void Dispose()
    {
        if (IsRunning)
            Stop();
        
        OnDispose();
    }

    protected virtual void OnDispose()
    {
        // empty
    }
    
    public void Start()
    {
        if (IsRunning)
            throw new InvalidOperationException("NetworkedStateSynchronizer is already started.");
        IsRunning = true;

        NetEntity.OnEntityDelete += OnEntityDeleteHandler;

        RelayClient.OnJoinedArea += OnJoinedAreaHandler;
        RelayClient.OnLeftArea += OnLeftAreaHandler;
        RelayClient.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        RelayClient.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        RelayClient.OnRequestedJoinArea += OnRequestedJoinAreaHandler;
        RelayClient.OnRequestedLeaveArea += OnRequestedLeaveAreaHandler;

        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsSnapshot, OnEcsSnapshotHandler);
        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsUpdate, OnEcsUpdateHandler);
        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsDeleteEntity, OnEcsDeleteEntityHandler);

        // NOTE: iterate over all components with generics without reflection
        netComponentRegistry.Accept(new RegisterJobRegistryCallback(this));
        netComponentRegistry.Accept(new RegisterSystemCallback(this));
    }

    public void Stop()
    {
        if (!IsRunning)
            throw new InvalidOperationException("NetworkedStateSynchronizer is not started.");
        IsRunning = false;

        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsDeleteEntity, OnEcsDeleteEntityHandler);
        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsUpdate, OnEcsUpdateHandler);
        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsSnapshot, OnEcsSnapshotHandler);

        RelayClient.OnRequestedLeaveArea -= OnRequestedLeaveAreaHandler;
        RelayClient.OnRequestedJoinArea -= OnRequestedJoinAreaHandler;
        RelayClient.OnOtherPlayerLeftArea -= OnOtherPlayerLeftAreaHandler;
        RelayClient.OnOtherPlayerJoinedArea -= OnOtherPlayerJoinedAreaHandler;
        RelayClient.OnLeftArea -= OnLeftAreaHandler;
        RelayClient.OnJoinedArea -= OnJoinedAreaHandler;

        NetEntity.OnEntityDelete -= OnEntityDeleteHandler;
    }
    
    #region Event handlers

    protected virtual void OnJoinedAreaHandler(IRelayClientNetworkThreadContext context, AreaId areaId)
    {
        OnJoinedArea?.Invoke();
        OnLateJoinedArea?.Invoke();
    }

    private void OnLeftAreaHandler(IRelayClientNetworkThreadContext obj)
    {
        OnLeftArea?.Invoke();
    }

    protected virtual void OnOtherPlayerJoinedAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        OnOtherPlayerJoinedArea?.Invoke(playerId);
    }
    
    protected virtual void OnOtherPlayerLeftAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        OnOtherPlayerLeftArea?.Invoke(playerId);
    }
    
    protected virtual void OnRequestedJoinAreaHandler(AreaId areaId)
    {
        // empty
    }

    protected virtual void OnRequestedLeaveAreaHandler()
    {
        // empty
    }

    protected void OnEcsSnapshotHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        ecsLoop.Scheduler.Schedule((_, self, readerCopy) =>
        {
            while (readerCopy.TryGetNetworkedComponentId(out var componentId))
            {
                if (!self.JobRegistry.SnapshotReaderJobs.TryGetValue(componentId, out var readerJob) || readerJob == null)
                {
                    self.Logger.LogError("No snapshot reader job registered for component ID: {Id}", componentId);
                    return;
                }

                readerJob.Execute(readerCopy);
            }
            
            self.OnEcsSnapshot?.Invoke();
        }, this, ecsLoop.Scheduler.MakeSafe(reader));
    }
    
    protected void OnEcsUpdateHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        ecsLoop.Scheduler.Schedule((_, self, readerCopy) =>
        {
            var componentId = readerCopy.Get<NetworkedComponentId>();
            if (!self.JobRegistry.DeltaReaderJobs.TryGetValue(componentId, out var readerJob) || readerJob == null)
            {
                self.Logger.LogError("No delta reader job registered for component ID: {Id}", componentId);
                return;
            }

            readerJob.Execute(readerCopy);
        }, this, ecsLoop.Scheduler.MakeSafe(reader));
    }

    protected void OnEcsDeleteEntityHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        var netId = reader.Get<NetworkId>();
        ecsLoop.Scheduler.Schedule((cb, self, netId0) =>
        {
            if (self.NetEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self.Logger.LogDebug("Queueing remote entity for destruction: {Id}", netId0);
                cb.DeleteEntity(entity.Value.Id);
            }
            else
            {
                self.Logger.LogError("Received destroy event for locally non-existent entity: {Id}", netId0);
            }
        }, this, netId);
    }

    protected void OnEntityDeleteHandler(NetworkId netId)
    {
        if (netId.Creator == RelayClient.PlayerId)
        {
            // our own entity - send destroy event
            Logger.LogDebug("Networked entity destroyed: {NetworkId} (owned)", netId);
            RelayClient.SendMessageRelayMode(RelayMessageCode.EcsDeleteEntity, netId, RelayMode.AreaOfInterestOthers, DeliveryMethod.ReliableOrdered); // TODO: Use RPC API instead
        }
    }

    #endregion
}
