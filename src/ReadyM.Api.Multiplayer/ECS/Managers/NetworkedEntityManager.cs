using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

internal sealed class NetworkedEntityManager : INetworkedEntityManager, IDisposable
{
    private readonly Store _world;
    private readonly CommandBuffer _commandBuffer;
    private readonly IPlayerIdProvider _playerIdProvider;
    private readonly ILogger _logger;

    private readonly ComponentIndex<MetadataComponent, NetworkId> _ix;
    private readonly HashSet<NetworkId> _netIdTombstones = [];

    private uint _nextNetworkedId;

    // NOTE: This event will be fired on the ECS thread.
    public event Action<NetworkId, Entity>? OnEntityDelete;

    public NetworkedEntityManager(
        Store world,
        IPlayerIdProvider playerIdProvider,
        ILogger logger)
    {
        _world = world;
        _playerIdProvider = playerIdProvider;
        _commandBuffer = world.GetCommandBuffer();
        _commandBuffer.ReuseBuffer = true;
        _logger = logger;

        _ix = _world.ComponentIndex<MetadataComponent, NetworkId>();

        // it's fine to subscribe here, since this is the only class that can create entities with MetadataComponent, so we won't miss any events
        _world.OnEntityDelete += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _world.OnEntityDelete -= OnEntityDeleteHandler;
    }

    public void SetNextNetworkedId(uint nextId)
    {
        _nextNetworkedId = nextId;
    }

    public bool IsNetworkEntityDeleted(NetworkId netId)
    {
        return _netIdTombstones.Contains(netId);
    }

    private int _skipNetSync;

    private void OnEntityDeleteHandler(EntityDelete evt)
    {
        if (evt.Entity.TryGetComponent<MetadataComponent>(out var meta))
        {
            _netIdTombstones.Add(meta.NetId);

            if (_skipNetSync == 0)
                OnEntityDelete?.Invoke(meta.NetId, evt.Entity);

            _logger.LogDebug("Network entity {Archetype} {NetId} deleted", meta.Archetype, meta.NetId);
        }
    }

    public (Entity Entity, NetworkId NetId) CreateNetworkedEntity(
        ArchetypeId archetypeId,
        Entity? scopeEntity,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null)
    {
        var playerId = _playerIdProvider.PlayerId;
        if (playerId == null)
            throw new InvalidOperationException();

        var netId = new NetworkId(playerId.Value, ++_nextNetworkedId);
        var owner = ownerOverride ?? netId.Creator;
        var meta = new MetadataComponent(netId, archetypeId, owner);
        var entity = _world.CreateEntity(archetypeId, b =>
        {
            b.Add(meta);
            if (scopeEntity != null)
            {
                var scope = new InScopeComponent(scopeEntity.Value);
                b.Add(scope);
            }

            // NOTE: This is added "temporarily" in order to mark the entity as not yet propagated over the network
            // Once the entity is propagated, this tag gets removed.
            b.AddTag<LocallyCreatedEntityTag>();
            setComponents?.Invoke(b);
        });

        _logger.LogDebug("Network entity {Archetype} {NetId} created (locally)", meta.Archetype, meta.NetId);

        return (entity, netId);
    }

    public Entity CreateRemoteNetworkedEntity(MetadataComponent meta, Entity? scopeEntity)
    {
        var entity = _world.CreateEntity(meta.Archetype, b =>
        {
            b.Add(meta);
            if (scopeEntity != null)
            {
                var scope = new InScopeComponent(scopeEntity.Value);
                b.Add(scope);
            }
        });

        _logger.LogDebug("Network entity {Archetype} {NetId} created (remote)", meta.Archetype, meta.NetId);

        return entity;
    }

    public bool TryGetEntityByNetworkId(NetworkId netId, [NotNullWhen(true)] out Entity? entity)
    {
        var matching = _ix[netId];

        switch (matching.Count)
        {
            case 0:
                entity = null;
                return false;
            case 1:
                entity = matching[0];
                return true;
            default:
                _logger.LogError("Multiple entities found with NetworkId {NetId}. This should not happen.", netId);
                entity = null;
                return false;
        }
    }

    public void DeleteEntitiesInScope(Entity scopeEntity, bool skipSync, bool deleteScopeEntity)
    {
        if (!scopeEntity.Tags.Has<ScopeEntityTag>())
            throw new InvalidOperationException("Entity is not a scope entity.");

        // NOTE: Scope related entity deletes are not synchronized over the network because each
        // client individually already deletes all those entities on their own. Having them also
        // synchronize using the normal EcsDeleteEntity events would result in an attempt to delete
        // the same entities twice. It would also waste a whole lot of traffic.
        if (skipSync)
            _skipNetSync++;
        // NOTE: Deleting all scope entities "atomically" so that they don't accidentally become global without their
        // InScopeComponent links.
        _world.Query<MetadataComponent>()
            .HasValue<InScopeComponent, Entity>(scopeEntity)
            .ForEachEntity((ref meta, entity) => { _commandBuffer.DeleteEntity(entity.Id); });
        _commandBuffer.Playback();

        if (deleteScopeEntity)
            scopeEntity.DeleteEntity();

        if (skipSync)
            _skipNetSync--;
    }

    public void DeleteAllNetworkedEntities(bool skipSync)
    {
        if (skipSync)
            _skipNetSync++;
        // When we disconnect all networked entities get deleted

        _world.Query<MetadataComponent>()
            .ForEachEntity((ref _, entity) => { _commandBuffer.DeleteEntity(entity.Id); });
        _commandBuffer.Playback();

        if (skipSync)
            _skipNetSync--;
    }

    public bool TryDeleteEntity(int entityId)
    {
        if (!_world.TryGetEntityById(entityId, out var entity))
        {
            _logger.LogWarning("Attempted to delete entity {EntityId} which does not exist.", entityId);
            return false;
        }

        if (entity.Tags.Has<ScopeEntityTag>())
        {
            _logger.LogError("Attempted to delete scope entity {EntityId}. Scope entities are owned by the server.", entityId);
            return false;
        }

        // Local entities carry no MetadataComponent, so the delete broadcast in
        // OnEntityDeleteHandler skips them and only networked deletes reach clients
        entity.DeleteEntity();
        return true;
    }
}
