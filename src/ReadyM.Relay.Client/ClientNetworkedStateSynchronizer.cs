using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client.ECS.Systems;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Jobs;

namespace ReadyM.Relay.Client;

public class ClientNetworkedStateSynchronizer : IDisposable
{
    protected IClientEcsUpdateLoop EcsLoop => _ecsLoop;

    private class RegisterSystemCallback(ClientNetworkedStateSynchronizer owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptComponent<T>(INetworkedComponentRegistry registry, T defaultValue = default)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();
            var deliveryMethod = registry.GetNetworkedComponentDeliveryMethod<T>();

            owner.Logger.LogDebug("Registering client send for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner._systemGroup.Add(new ClientSendComponentDeltaSystem<T>(id, deliveryMethod, owner.RelayClient));
        }
    }

    protected readonly ClientState State;
    protected readonly NetworkedEntityManager NetEntity;
    protected readonly IRelayClient RelayClient;
    protected readonly ILogger Logger;

    protected readonly JobRegistry JobRegistry;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly ClientOwnershipManager _ownershipManager;

    private readonly SystemGroup _systemGroup;

    public ClientNetworkedStateSynchronizer(NetworkedEntityManager netEntity,
        ClientState state,
        JobRegistry jobRegistry,
        INetworkedComponentRegistry netComponentRegistry,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        ClientOwnershipManager ownershipManager,
        ILogger logger)
    {
        State = state;
        _ecsLoop = ecsLoop;
        _ownershipManager = ownershipManager;
        NetEntity = netEntity;
        RelayClient = relayClient;
        Logger = logger;
        JobRegistry = jobRegistry;

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

        // NOTE: when an entity is created locally on the client, it's marked with a special tag that allows it to be
        // filtered out by the `ClientSendEntityCreatedSystem`. For all newly created entities, a message is sent to the
        // server.

        _systemGroup = new SystemGroup("Network");

        _systemGroup.Add(new ClientSendEntityCreatedSystem(jobRegistry, state, relayClient));
        // NOTE: iterates over all network components with generics without reflection
        netComponentRegistry.Accept(new RegisterSystemCallback(this));

        _systemGroup.SetMonitorPerf(true);
        _ecsLoop.AddSystem(_systemGroup);
    }

    public void Dispose()
    {
        OnDispose();
    }

    protected virtual void OnDispose()
    {
        _ecsLoop.RemoveSystem(_systemGroup);

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

    protected void OnEcsSnapshotMessageHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        _ecsLoop.Scheduler.Schedule(static (context0, self, readerCopy) =>
        {
            try
            {
                _skipEcsEventMessages++;

                var scopeNetId = readerCopy.Get<NetworkId>();
                Entity? scopeEntity = null;
                
                var entityCount = readerCopy.GetUInt();
                for (var i = 0; i < entityCount; i++)
                {
                    var meta = readerCopy.Get<MetadataComponent>();

                    if (!self.NetEntity.TryGetEntityByNetworkId(meta.NetId, out var entity))
                    {
                        self.NetEntity.CreateRemoteNetworkedEntity(meta, scopeEntity);
                    }
                    else
                    {
                        self.Logger.LogError("Received snapshot create event for already existing entity: {Id} scope: {Scope}, already existing {Existing}", meta.NetId, scopeNetId, entity);
                    }
                    
                    if (i == 0 && scopeNetId != default)
                    {
                        // NOTE: The scope entity is always the first being created
                        
                        self.Logger.LogInformation("Looking up scope entity with NetId {ScopeNetId}", scopeNetId);
                        if (!self.NetEntity.TryGetEntityByNetworkId(scopeNetId, out scopeEntity))
                            throw new InvalidOperationException($"Scope entity with NetId {scopeNetId} not found");
                    }
                }

                self.JobRegistry.ApplySnapshot(readerCopy);

                if (scopeEntity != null)
                {
                    self.Logger.LogInformation("Scope entity after applying snapshot: {ScopeEntity}", scopeEntity);
                }
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _ecsLoop.Scheduler.MakeSafe(reader));
    }

    protected void OnEcsChangeOwnershipMessageHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        _ecsLoop.Scheduler.Schedule(static (context0, self, readerCopy) =>
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
                        self.Logger.LogWarning("Received ownership transfer event for non-existent entity: {Id}", netId);
                    }
                }
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _ecsLoop.Scheduler.MakeSafe(reader));
    }

    protected void OnEcsDeltaMessageHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, readerCopy) =>
        {
            try
            {
                _skipEcsEventMessages++;
                self.JobRegistry.ApplyDelta(readerCopy);
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _ecsLoop.Scheduler.MakeSafe(reader));
    }

    // NOTE: Someone else created an entity, and we are notified about it
    protected void OnEcsCreateEntityMessageHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        _ecsLoop.Scheduler.Schedule(static (cb, self, readerCopy) =>
        {
            try
            {
                _skipEcsEventMessages++;

                var scopeNetId = readerCopy.Get<NetworkId>();
                Entity? scopeEntity = null;
                if (scopeNetId != default)
                {
                    if (!self.NetEntity.TryGetEntityByNetworkId(scopeNetId, out scopeEntity))
                        throw new InvalidOperationException($"Scope entity with NetId {scopeNetId} not found");
                }

                var queryCount = readerCopy.GetUInt();
                for (var i = 0; i < queryCount; i++)
                {
                    var meta = readerCopy.Get<MetadataComponent>();
                    if (!self.NetEntity.TryGetEntityByNetworkId(meta.NetId, out var entity))
                    {
                        self.NetEntity.CreateRemoteNetworkedEntity(meta, scopeEntity);
                    }
                    else
                    {
                        self.Logger.LogError("Received create event for already existing entity: {Id}", meta.NetId);
                    }
                }

                self.JobRegistry.ApplySnapshot(readerCopy);
                
                if (scopeEntity != null)
                {
                    self.Logger.LogInformation("Scope entity after applying snapshot: {ScopeEntity}", scopeEntity);
                }
            }
            finally
            {
                _skipEcsEventMessages--;
            }
        }, this, _ecsLoop.Scheduler.MakeSafe(reader));
    }

    // NOTE: Someone else deleted an entity, and we are notified about it
    protected void OnEcsDeleteEntityMessageHandler(IRelayClientNetworkThreadContext context, ServerEventHeader header, NetDataReader reader)
    {
        var netId = reader.Get<NetworkId>();
        _ecsLoop.Scheduler.Schedule(static (cb, self, netId0) =>
        {
            try
            {
                _skipEcsEventMessages++;
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

        _ecsLoop.Scheduler.EnsureThread();

        if (!_ownershipManager.OwnsEntity(netId))
            return;

        // Our own entity - send destroy event to the server. The server will react by deleting it on the server and
        // resending a separate message to the other clients
        Logger.LogDebug("Networked entity destroyed: {NetworkId} (owned)", netId);
        RelayClient.SendMessageToServer(RelayMessageCode.EcsDeleteEntity, netId, DeliveryMethod.ReliableOrdered);
    }

    #endregion
}