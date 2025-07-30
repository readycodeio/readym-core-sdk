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

// There's some confusion as to the split of responsibilities between the RelayClient and the rest of the code base
// (formerly ReadyMultiplayerMod and its subclasses; and now NetworkedStateSynchronizer). We have to decide what each
// class is responsible for. For example, RelayClient doesn't call SendInitialPlayerState() on itself when the room
// is joined. But it does call other sync protocol-related methods.
// 
// Proposal: RelayClient should be an event-bus with minimal internal logic.
// NetworkedSynchronizer should be the part that subscribes to various events from multiple places and holistically
// managed synchronization by triggering events and responding to events.
public class ClientNetworkedStateSynchronizer(
    Store world,
    NetworkedEntityManager netEntity,
    ClientJobRegistry jobRegistry,
    INetworkedComponentRegistry netComponentRegistry,
    IRelayClient relayClient, 
    IClientEcsUpdateLoop updateLoop,
    ILogger logger) : IDisposable
{
    private class RegisterJobRegistryCallback(ClientNetworkedStateSynchronizer owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptComponent<T>(INetworkedComponentRegistry registry)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();

            owner._logger.LogDebug("Registering jobs for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner._jobRegistry.DeltaReaderJobs.Add(id, new ApplyDeltaJob<T>(owner._netEntity, () => owner._relayClient.PlayerId));
            owner._jobRegistry.SnapshotReaderJobs.Add(id, new ApplySnapshotJob<T>(owner._netEntity));
        }
    }
    
    private class RegisterSystemCallback(ClientNetworkedStateSynchronizer owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptComponent<T>(INetworkedComponentRegistry registry)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();
            
            owner._logger.LogDebug("Registering client send for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner._world.SystemRoot.Add(new ClientSendComponentDeltaSystem<T>(id, owner._relayClient));
        }
    }

    private readonly Store _world = world;
    private readonly NetworkedEntityManager _netEntity = netEntity;
    private readonly IRelayClient _relayClient = relayClient;
    private readonly ILogger _logger = logger;
    
    private readonly ClientJobRegistry _jobRegistry = jobRegistry;
    
    public bool IsRunning { get; private set; }
    
    public event Action? OnJoinedArea;
    public event Action? OnLateJoinedArea;
    public event Action? OnLeftArea;
    public event Action<PlayerId>? OnOtherPlayerJoinedArea;
    public event Action<PlayerId>? OnOtherPlayerLeftArea;

    public event Action? OnEcsSnapshot;
    
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

        _netEntity.OnEntityDelete += OnEntityDeleteHandler;

        _relayClient.OnJoinedArea += OnJoinedAreaHandler;
        _relayClient.OnLeftArea += OnLeftAreaHandler;
        _relayClient.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        _relayClient.OnRequestedJoinArea += OnRequestedJoinAreaHandler;
        _relayClient.OnRequestedLeaveArea += OnRequestedLeaveAreaHandler;

        _relayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsSnapshot, OnEcsSnapshotHandler);
        _relayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsUpdate, OnEcsUpdateHandler);
        _relayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsDeleteEntity, OnEcsDeleteEntityHandler);

        // NOTE: iterate over all components with generics without reflection
        netComponentRegistry.Accept(new RegisterJobRegistryCallback(this));
        netComponentRegistry.Accept(new RegisterSystemCallback(this));
    }

    public void Stop()
    {
        if (!IsRunning)
            throw new InvalidOperationException("NetworkedStateSynchronizer is not started.");
        IsRunning = false;

        _relayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsDeleteEntity, OnEcsDeleteEntityHandler);
        _relayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsUpdate, OnEcsUpdateHandler);
        _relayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsSnapshot, OnEcsSnapshotHandler);

        _relayClient.OnRequestedLeaveArea -= OnRequestedLeaveAreaHandler;
        _relayClient.OnRequestedJoinArea -= OnRequestedJoinAreaHandler;
        _relayClient.OnOtherPlayerLeftArea -= OnOtherPlayerLeftAreaHandler;
        _relayClient.OnOtherPlayerJoinedArea -= OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnLeftArea -= OnLeftAreaHandler;
        _relayClient.OnJoinedArea -= OnJoinedAreaHandler;

        _netEntity.OnEntityDelete -= OnEntityDeleteHandler;
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

    protected virtual void OnEcsSnapshotHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        updateLoop.Scheduler.Schedule((cb, self, readerCopy) =>
        {
            while (readerCopy.TryGetNetworkedComponentId(out var componentId))
            {
                if (!self._jobRegistry.SnapshotReaderJobs.TryGetValue(componentId, out var readerJob) || readerJob == null)
                {
                    self._logger.LogError("No snapshot reader job registered for component ID: {Id}", componentId);
                    return;
                }

                readerJob.Execute(readerCopy);
            }
            
            self.OnEcsSnapshot?.Invoke();
        }, this, updateLoop.Scheduler.MakeSafe(reader));
    }
    
    protected virtual void OnEcsUpdateHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        updateLoop.Scheduler.Schedule((_, self, readerCopy) =>
        {
            var componentId = readerCopy.Get<NetworkedComponentId>();
            if (!self._jobRegistry.DeltaReaderJobs.TryGetValue(componentId, out var readerJob) || readerJob == null)
            {
                self._logger.LogError("No delta reader job registered for component ID: {Id}", componentId);
                return;
            }

            readerJob.Execute(readerCopy);
        }, this, updateLoop.Scheduler.MakeSafe(reader));
    }

    protected virtual void OnEcsDeleteEntityHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        var netId = reader.Get<NetworkIdComponent>();
        if (_netEntity.TryGetEntityByNetworkId(netId, out var entity))
        {
            _logger.LogDebug("Queueing remote entity for destruction: {Id}", netId);
            updateLoop.Scheduler.Schedule((cb, entity0) =>
            {
                cb.DeleteEntity(entity0!.Value.Id);
            }, entity);
        }
        else
        {
            _logger.LogError("Received destroy event for locally non-existent entity: {Id}", netId);
        }
    }

    protected virtual void OnEntityDeleteHandler(NetworkIdComponent netId)
    {
        if (netId.Creator == _relayClient.PlayerId)
        {
            // our own entity - send destroy event
            _logger.LogDebug("Networked entity destroyed: {Id} (owned)", netId);
            _relayClient.SendMessageRelayMode(RelayMessageCode.EcsDeleteEntity, netId, RelayMode.AreaOfInterestOthers, DeliveryMethod.ReliableOrdered); // TODO: Use RPC API instead
        }
    }

    #endregion
}
