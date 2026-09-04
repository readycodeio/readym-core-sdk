using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Systems;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client.ConflictResolution;
using ReadyM.Relay.Client.ECS.Systems;
using ReadyM.Relay.Client.State;

namespace ReadyM.Relay.Client;

internal class ClientNetworkedStateSynchronizer : IHostedService
{
    private class RegisterSystemCallback(ClientNetworkedStateSynchronizer owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptModComponent(INetworkedComponentRegistry registry, ModComponentRegistration registration, string typeFullName)
            => throw new NotSupportedException(
                $"{nameof(AcceptModComponent)} is not supported here: the client does not load server mods, so it never sees a mod component. "
                + $"Offending component: {typeFullName}.");

        public void AcceptComponent<T>(INetworkedComponentRegistry registry, T defaultValue = default)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();
            var deliveryMethod = registry.GetNetworkedComponentDeliveryMethod<T>();

            owner.Logger.LogTrace("Registering client send for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner.SendSystemGroup.Add(new ClientSendComponentDeltaSystem<T>(id, owner._netTime, deliveryMethod, owner.RelayClient));
            owner._clearDirtySystemGroup.Add(new ClearDirtySystem<T>());
        }
    }

    protected readonly ClientState State;
    protected readonly INetworkedEntityManager NetEntity;
    protected readonly IRelayClient RelayClient;
    protected readonly ILogger Logger;

    private readonly IClientNetworkTime _netTime;
    protected readonly SerializationJobRegistry SerializationJobRegistry;
    private readonly ClientEcsUpdateLoop _ecsLoop;
    private readonly ClientOwnershipManager _ownershipManager;
    private readonly ReceiveSystem _receiveSystem;
    private readonly INetworkedComponentRegistry _netComponentRegistry;

    private readonly SystemGroup _clearDirtySystemGroup;
    private readonly Dictionary<NetworkId, PlayerId> _pendingOwnershipTransfers = [];

    protected SystemGroup ReceiveSystemGroup { get; }

    protected SystemGroup SendSystemGroup { get; }

    protected SystemGroup SyncSystemGroup { get; }

    public ClientNetworkedStateSynchronizer(INetworkedEntityManager netEntity,
        IClientNetworkTime netTime,
        ClientState state,
        SerializationJobRegistry serializationJobRegistry,
        INetworkedComponentRegistry netComponentRegistry,
        IRelayClient relayClient,
        ReceiveSystem receiveSystem,
        ClientEcsUpdateLoop ecsLoop,
        ClientOwnershipManager ownershipManager,
        ILogger logger)
    {
        State = state;
        _netTime = netTime;
        _receiveSystem = receiveSystem;
        _ecsLoop = ecsLoop;
        _ownershipManager = ownershipManager;
        _netComponentRegistry = netComponentRegistry;
        NetEntity = netEntity;
        RelayClient = relayClient;
        Logger = logger;
        this.SerializationJobRegistry = serializationJobRegistry;

        // NOTE: when an entity is created locally on the client, it's marked with a special tag that allows it to be
        // filtered out by the `ClientSendEntityCreatedSystem`. For all newly created entities, a message is sent to the
        // server.

        ReceiveSystemGroup = new SchedulerSystemGroup("Receive", _receiveSystem);
#if DEBUG
        ReceiveSystemGroup.SetMonitorPerf(true);
#endif

        SyncSystemGroup = new SystemGroup("Sync");
#if DEBUG
        SyncSystemGroup.SetMonitorPerf(true);
#endif


        SendSystemGroup = new SystemGroup("Send");
#if DEBUG
        SendSystemGroup.SetMonitorPerf(true);
#endif

        _clearDirtySystemGroup = new SystemGroup("ClearDirty");
#if DEBUG
        _clearDirtySystemGroup.SetMonitorPerf(true);
#endif
    }

    public virtual void OnScopeStart()
    {
        // When an ECS snapshot message is received, the client applies it to its ECS world. No response is sent to the server.
        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsSnapshot, OnEcsSnapshotMessageHandler);

        // When an ECS delta message is received, the client applies it to its ECS world. No response is sent to the server.
        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsDelta, OnEcsDeltaMessageHandler);

        // When an ECS create entity message is received, the client creates a new entity in its ECS world. No response is sent to the server.
        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsCreateEntity, OnEcsCreateEntityMessageHandler);

        // When an ECS delete entity message is received, the client deletes the entity from its ECS world. No response is sent to the server.
        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsDeleteEntity, OnEcsDeleteEntityMessageHandler);

        // When an ECS change ownership message is received, the client updates the ownership of the entity in its ECS world. No response is sent to the server.
        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsChangeOwnership, OnEcsChangeOwnershipMessageHandler);

        // When an entity is deleted, we check if the event originated locally on the client. If yes, then a message is
        // sent to the server.
        NetEntity.OnEntityDelete += OnEntityDeleteHandler;

        _ecsLoop.AddSystem(ReceiveSystemGroup);
        _ecsLoop.AddSystem(SyncSystemGroup);
        _ecsLoop.AddSystem(SendSystemGroup);
        _ecsLoop.AddSystem(_clearDirtySystemGroup);

        ReceiveSystemGroup.Add(_receiveSystem);
        SyncSystemGroup.Add(State.System);
        SendSystemGroup.Add(new ClientSendEntityCreatedSystem(SerializationJobRegistry, State, RelayClient));

        // NOTE: iterates over all network components with generics without reflection
        _netComponentRegistry.Accept(new RegisterSystemCallback(this));
    }

    public void Dispose()
    {
        OnDispose();
    }

    protected virtual void OnDispose()
    {
        _ecsLoop.RemoveSystem(SendSystemGroup);
        _ecsLoop.RemoveSystem(SyncSystemGroup);
        _ecsLoop.RemoveSystem(ReceiveSystemGroup);

        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsDeleteEntity, OnEcsDeleteEntityMessageHandler);
        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsCreateEntity, OnEcsCreateEntityMessageHandler);
        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsDelta, OnEcsDeltaMessageHandler);
        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsSnapshot, OnEcsSnapshotMessageHandler);
        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsChangeOwnership, OnEcsChangeOwnershipMessageHandler);

        NetEntity.OnEntityDelete -= OnEntityDeleteHandler;
    }

    protected virtual void OnOwnershipChanged(Entity entity) { }

    #region Event handlers

    // NOTE: This static variable is used as a side channel to communicate that a network event is being processed.
    // This is in order to prevent events triggered by the ECS world from sending out spurious secondary messages to
    // the server.
    [ThreadStatic]
    private static int _skipEcsEventMessages;

    private void ApplyPendingOwnershipTransfer(NetworkId netId)
    {
        if (!_pendingOwnershipTransfers.Remove(netId, out var owner))
            return;

        if (!NetEntity.TryGetEntityByNetworkId(netId, out var entity))
            return;

        entity.Value.GetComponent<MetadataComponent>().Owner = owner;
        OnOwnershipChanged(entity.Value);
        Logger.LogInformation("Applied parked ownership transfer for entity {Id}", netId);
    }

    protected void OnEcsSnapshotMessageHandler(ServerEventHeader header, NetDataReader reader)
    {
        _receiveSystem.Scheduler.Schedule(static (_, self, readerCopy) =>
        {
            try
            {
                _skipEcsEventMessages++;

                var scopeNetId = readerCopy.Get<NetworkId>();
                Entity? scopeEntity = null;

                var entityCount = readerCopy.GetUInt();

                for (var i = 0; i < entityCount; i++)
                {
                    var meta = MetadataComponent.Deserialize(readerCopy);

                    if (!self.NetEntity.TryGetEntityByNetworkId(meta.NetId, out var _))
                    {
                        self.NetEntity.CreateRemoteNetworkedEntity(meta, scopeEntity);
                        self.ApplyPendingOwnershipTransfer(meta.NetId);
                    }
                    else
                    {
                        self.Logger.LogError("Received snapshot create event for already existing entity: {Id} scope: {Scope}", meta.NetId, scopeNetId);
                    }

                    if (i == 0 && scopeNetId != default)
                    {
                        // NOTE: The scope entity is always the first being created

                        self.Logger.LogInformation("Looking up scope entity with NetId {ScopeNetId}", scopeNetId);
                        if (!self.NetEntity.TryGetEntityByNetworkId(scopeNetId, out scopeEntity))
                            throw new InvalidOperationException($"Scope entity with NetId {scopeNetId} not found");
                    }
                }

                self.SerializationJobRegistry.ApplySnapshot(readerCopy);
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _receiveSystem.Scheduler.MakeSafe(reader));
    }

    protected void OnEcsChangeOwnershipMessageHandler(ServerEventHeader header, NetDataReader reader)
    {
        _receiveSystem.Scheduler.Schedule(static (context0, self, readerCopy) =>
        {
            try
            {
                _skipEcsEventMessages++;
                var newOwner = readerCopy.Get<PlayerId>();

                while (readerCopy.TryGetNetworkId(out var netId))
                {
                    if (self.NetEntity.TryGetEntityByNetworkId(netId, out var entity))
                    {
                        entity.Value.GetComponent<MetadataComponent>().Owner = newOwner;
                        self.OnOwnershipChanged(entity.Value);
                    }
                    else
                    {
                        self._pendingOwnershipTransfers[netId] = newOwner;
                        self.Logger.LogInformation("Parked ownership transfer for not yet created entity: {Id}", netId);
                    }
                }
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _receiveSystem.Scheduler.MakeSafe(reader));
    }

    protected void OnEcsDeltaMessageHandler(ServerEventHeader header, NetDataReader reader)
    {
        _receiveSystem.Scheduler.Schedule(static (_, self, readerCopy) =>
        {
            try
            {
                _skipEcsEventMessages++;
                var serverTime = readerCopy.GetUInt();
                self._netTime.SetObservedTime(serverTime);
                self.SerializationJobRegistry.ApplyDelta(readerCopy);
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _receiveSystem.Scheduler.MakeSafe(reader));
    }

    // NOTE: Someone else created an entity, and we are notified about it
    protected void OnEcsCreateEntityMessageHandler(ServerEventHeader header, NetDataReader reader)
    {
        _receiveSystem.Scheduler.Schedule(static (cb, self, readerCopy) =>
        {
            try
            {
                _skipEcsEventMessages++;

                var scopeNetId = readerCopy.Get<NetworkId>();
                Entity? scopeEntity = null;
                if (scopeNetId != default)
                {
                    if (!self.NetEntity.TryGetEntityByNetworkId(scopeNetId, out scopeEntity))
                    {
                        // NOTE: This situation is possible when a new client enters the game and is forwarded entities
                        // created by another player before receiving the corresponding snapshot
                        self.Logger.LogDebug("Scope entity with NetId {Scope} not found", scopeNetId);
                        return;
                    }
                }

                var queryCount = readerCopy.GetUInt();
                for (var i = 0; i < queryCount; i++)
                {
                    var meta = MetadataComponent.Deserialize(readerCopy);
                    if (!self.NetEntity.TryGetEntityByNetworkId(meta.NetId, out var entity))
                    {
                        self.NetEntity.CreateRemoteNetworkedEntity(meta, scopeEntity);
                        self.ApplyPendingOwnershipTransfer(meta.NetId);
                    }
                    else
                    {
                        self.Logger.LogError("Received create event for already existing entity: {Id}", meta.NetId);
                    }
                }

                self.SerializationJobRegistry.ApplySnapshot(readerCopy);
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _receiveSystem.Scheduler.MakeSafe(reader));
    }

    // NOTE: Someone else deleted an entity, and we are notified about it
    protected void OnEcsDeleteEntityMessageHandler(ServerEventHeader header, NetDataReader reader)
    {
        var netId = reader.Get<NetworkId>();
        _receiveSystem.Scheduler.Schedule(static (cb, self, netId0) =>
        {
            try
            {
                _skipEcsEventMessages++;
                self._pendingOwnershipTransfers.Remove(netId0);
                if (self.NetEntity.TryGetEntityByNetworkId(netId0, out var entity))
                {
                    self.Logger.LogDebug("Deleting remote entity: {Id}", netId0);
                    cb.DeleteEntity(entity.Value.Id);
                }
                else
                {
                    self.Logger.LogWarning("Received destroy event for locally non-existent entity: {Id}", netId0);
                }
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, netId);
    }

    // NOTE: We deleted the entity, and we need to message the server about it
    protected void OnEntityDeleteHandler(NetworkId netId, Entity entity)
    {
        if (_skipEcsEventMessages > 0)
            return;

        _receiveSystem.Scheduler.EnsureThread();

        if (!_ownershipManager.OwnsEntity(netId))
            return;

        // Our own entity - send destroy event to the server. The server will react by deleting it on the server and
        // resending a separate message to the other clients
        Logger.LogDebug("Networked entity destroyed: {NetworkId} (owned)", netId);
        RelayClient.SendMessageToServer(RelayMessageCode.EcsDeleteEntity, netId, DeliveryMethod.ReliableOrdered);
    }

    #endregion
}