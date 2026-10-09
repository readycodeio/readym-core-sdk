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
using ReadyM.Api.Mapping.Tags;
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
        public void AcceptModComponent(INetworkedComponentRegistry registry, ModComponentInfo info, string typeFullName)
            => throw new NotSupportedException(
                $"{nameof(AcceptModComponent)} is not supported here: the client does not load server mods, so it never sees a mod component. "
                + $"Offending component: {typeFullName}.");

        public void AcceptComponent<T>(INetworkedComponentRegistry registry, T defaultValue = default)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();
            var deliveryMethod = registry.GetNetworkedComponentDeliveryMethod<T>();

            if (!typeof(IServerAuthoritative).IsAssignableFrom(typeof(T)))
            {
                owner.Logger.LogTrace("Registering client send for: {ComponentType} with ID {Id}", typeof(T).Name, id);
                owner.SendSystemGroup.Add(new ClientSendComponentDeltaSystem<T>(id, owner._netTime, deliveryMethod, owner.RelayClient));
            }

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
    private readonly HashSet<NetworkId> _deletesFromServer = [];
    private readonly Dictionary<NetworkId, List<NetDataReader>> _creationsWaitingForScope = [];
    private readonly HashSet<NetworkId> _createdFromHeldCreations = [];

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

        // When an ECS change scope message is received, the client moves an entity it already has into another scope. No response is sent to the server.
        RelayClient.AddBuiltInMessageHandler(RelayMessageCode.EcsChangeScope, OnEcsChangeScopeMessageHandler);

        // When an entity is deleted, we check if the event originated locally on the client. If yes, then a message is
        // sent to the server.
        NetEntity.OnEntityDelete += OnEntityDeleteHandler;
        State.OnLeftArea += OnLeftAreaHandler;

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
        RelayClient.RemoveBuiltInMessageHandler(RelayMessageCode.EcsChangeScope, OnEcsChangeScopeMessageHandler);

        NetEntity.OnEntityDelete -= OnEntityDeleteHandler;
        State.OnLeftArea -= OnLeftAreaHandler;
    }

    protected virtual void OnOwnershipChanged(Entity entity) { }

    #region Event handlers

    // NOTE: This static variable is used as a side channel to communicate that a network event is being processed.
    // This is in order to prevent events triggered by the ECS world from sending out spurious secondary messages to
    // the server.
    [ThreadStatic]
    private static int _skipEcsEventMessages;

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
                var created = new List<NetworkId>((int)entityCount);

                for (var i = 0; i < entityCount; i++)
                {
                    var meta = MetadataComponent.Deserialize(readerCopy);

                    if (!self.NetEntity.TryGetEntityByNetworkId(meta.NetId, out var _))
                    {
                        self.NetEntity.CreateRemoteNetworkedEntity(meta, scopeEntity);
                        created.Add(meta.NetId);
                    }
                    else if (!self._createdFromHeldCreations.Remove(meta.NetId))
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
                self.CreateEntitiesWaitingFor(created);
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
                        self.Logger.LogInformation("Ownership of entity {Id} changed from {OldOwner} to {Owner}", netId, entity.Value.GetComponent<MetadataComponent>().Owner, newOwner);
                        entity.Value.GetComponent<MetadataComponent>().Owner = newOwner;
                        self.OnOwnershipChanged(entity.Value);
                    }
                    else
                    {
                        self.Logger.LogInformation("Ignored ownership transfer to {Owner} for entity {Id} that does not exist here yet, its creation carries the current owner", newOwner, netId);
                    }
                }
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _receiveSystem.Scheduler.MakeSafe(reader));
    }

    protected void OnEcsChangeScopeMessageHandler(ServerEventHeader header, NetDataReader reader)
    {
        var netId = reader.Get<NetworkId>();
        var scopeNetId = reader.Get<NetworkId>();
        _receiveSystem.Scheduler.Schedule(static (_, self, netId0, scopeNetId0) =>
        {
            try
            {
                _skipEcsEventMessages++;
                if (!self.NetEntity.TryGetEntityByNetworkId(netId0, out var entity))
                {
                    self.Logger.LogWarning("Received change scope event for locally non-existent entity: {Id}", netId0);
                    return;
                }

                if (!self.NetEntity.TryGetEntityByNetworkId(scopeNetId0, out var scopeEntity))
                {
                    self.Logger.LogWarning("Received change scope event for entity {Id} into locally non-existent scope: {Scope}", netId0, scopeNetId0);
                    return;
                }

                // Changing the field directly would not update the scope index
                entity.Value.AddComponent(new InScopeComponent(scopeEntity.Value));
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, netId, scopeNetId);
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
                        self.HoldCreationUntilScope(scopeNetId, readerCopy);
                        return;
                    }
                }

                self.CreateEntitiesWaitingFor(self.CreateEntities(scopeEntity, readerCopy, false));
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _receiveSystem.Scheduler.MakeSafe(reader));
    }

    private List<NetworkId> CreateEntities(Entity? scopeEntity, NetDataReader reader, bool held)
    {
        var queryCount = reader.GetUInt();
        var created = new List<NetworkId>((int)queryCount);
        for (var i = 0; i < queryCount; i++)
        {
            var meta = MetadataComponent.Deserialize(reader);
            if (!NetEntity.TryGetEntityByNetworkId(meta.NetId, out _))
            {
                NetEntity.CreateRemoteNetworkedEntity(meta, scopeEntity);
                created.Add(meta.NetId);
                if (held)
                {
                    _createdFromHeldCreations.Add(meta.NetId);
                    Logger.LogInformation("Created held entity {Id} owned by {Owner}", meta.NetId, meta.Owner);
                }
            }
            else if (!held)
            {
                Logger.LogError("Received create event for already existing entity: {Id}", meta.NetId);
            }
        }

        if (held && created.Count == 0)
            return created;

        SerializationJobRegistry.ApplySnapshot(reader);
        return created;
    }

    private void HoldCreationUntilScope(NetworkId scopeNetId, NetDataReader reader)
    {
        if (!_creationsWaitingForScope.TryGetValue(scopeNetId, out var waiting))
        {
            waiting = [];
            _creationsWaitingForScope.Add(scopeNetId, waiting);
        }

        var bytes = reader.GetRemainingBytes();
        waiting.Add(new NetDataReader(bytes, 0, bytes.Length));
        Logger.LogInformation("Holding an entity creation until its scope {Scope} arrives", scopeNetId);
    }

    private void CreateEntitiesWaitingFor(List<NetworkId> createdNetIds)
    {
        foreach (var netId in createdNetIds)
        {
            if (!_creationsWaitingForScope.Remove(netId, out var waiting))
                continue;

            NetEntity.TryGetEntityByNetworkId(netId, out var scopeEntity);
            Logger.LogInformation("Scope {Scope} arrived, creating the {Count} entity creation(s) held for it", netId, waiting.Count);
            foreach (var reader in waiting)
                CreateEntitiesWaitingFor(CreateEntities(scopeEntity, reader, true));
        }
    }

    private void OnLeftAreaHandler(AreaId areaId, Entity areaEntity)
    {
        _createdFromHeldCreations.Clear();
        if (_creationsWaitingForScope.Count == 0)
            return;

        Logger.LogInformation("Dropping entity creations held for {Count} scope(s) that never arrived before leaving {Area}", _creationsWaitingForScope.Count, areaId);
        _creationsWaitingForScope.Clear();
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
                if (self.NetEntity.TryGetEntityByNetworkId(netId0, out var entity))
                {
                    self.Logger.LogDebug("Deleting remote entity: {Id}", netId0);
                    // The command buffer runs the delete after the skip counter is back to zero
                    self._deletesFromServer.Add(netId0);
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
        if (_deletesFromServer.Remove(netId))
            return;

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