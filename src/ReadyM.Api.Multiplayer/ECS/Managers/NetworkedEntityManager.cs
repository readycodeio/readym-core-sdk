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

public sealed class NetworkedEntityManager : IDisposable
{
    private readonly Store _world;
    private readonly IPlayerIdProvider _playerIdProvider;
    private readonly ILogger _logger;

    private readonly ComponentIndex<MetadataComponent, NetworkId> _ix;
    private readonly HashSet<NetworkId> _netIdTombstones = [];
    
    private uint _nextNetworkedId;
    
    // NOTE: This event will be fired on the ECS thread.
    public event Action<NetworkId>? OnEntityDelete;

    public NetworkedEntityManager(Store world, ILogger logger, IPlayerIdProvider playerIdProvider)
    {
        _world = world;
        _logger = logger;
        _playerIdProvider = playerIdProvider;
        
        _ix = _world.ComponentIndex<MetadataComponent, NetworkId>();

        _world.OnEntityDelete += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _world.OnEntityDelete -= OnEntityDeleteHandler;
    }

    public bool IsNetworkEntityDeleted(NetworkId netId)
    {
        return _netIdTombstones.Contains(netId);
    }

    private void OnEntityDeleteHandler(EntityDelete evt)
    {
        if (evt.Entity.TryGetComponent<MetadataComponent>(out var meta))
        {
            _netIdTombstones.Add(meta.NetId);
            OnEntityDelete?.Invoke(meta.NetId);
        }
    }

    public (Entity Entity, NetworkId NetId) CreateNetworkedEntity(
        ArchetypeId archetypeId,
        Entity? scopeEntity,
        Action<EntityBuilder>? setComponents = null)
    {
        var playerId = _playerIdProvider.PlayerId;
        if (playerId == null)
            throw new InvalidOperationException();
        
        var netId = new NetworkId(playerId.Value, ++_nextNetworkedId);
        var meta = new MetadataComponent(netId, archetypeId, netId.Creator);
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
        return (entity, netId);
    }

    public Entity CreateRemoteNetworkedEntity(MetadataComponent meta, Entity? scopeEntity)
    {
        return _world.CreateEntity(meta.Archetype, b =>
        {
            b.Add(meta);
            if (scopeEntity != null)
            {
                var scope = new InScopeComponent(scopeEntity.Value);
                b.Add(scope);
            }
        });
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
                _logger.LogError("Multiple entities found with NetworkId {NetworkId}. This should not happen.", netId);
                entity = null;
                return false;
        }
    }
}