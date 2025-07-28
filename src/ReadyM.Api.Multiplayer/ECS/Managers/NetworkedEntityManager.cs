using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

public sealed class NetworkedEntityManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly Store _store;
    private readonly Func<PlayerId> _getPlayerId;
    
    private uint _nextNetworkedId;
    
    // NOTE: This event will be fired on the ECS thread.
    public event Action<NetworkIdComponent>? OnEntityDelete;

    private readonly HashSet<NetworkIdComponent> _netIdTombstones = [];

    // FIXME: getPlayerId should be replaced with an interface IRelayClient that can be mocked in tests
    public NetworkedEntityManager(Store store, ILogger logger, Func<PlayerId> getPlayerId)
    {
        _store = store;
        _logger = logger;
        _getPlayerId = getPlayerId;
        _store.OnEntityDelete += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _store.OnEntityDelete -= OnEntityDeleteHandler;
    }

    public bool IsNetworkEntityDeleted(NetworkIdComponent networkId)
    {
        return _netIdTombstones.Contains(networkId);
    }

    private void OnEntityDeleteHandler(EntityDelete evt)
    {
        if (evt.Entity.TryGetComponent<MetadataComponent>(out var meta))
        {
            _netIdTombstones.Add(meta.NetId);
            OnEntityDelete?.Invoke(meta.NetId);
        }
    }

    public (Entity Entity, NetworkIdComponent NetId) CreateNetworkedGlobalEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null)
        => CreateNetworkedEntity(archetypeId, default, setComponents);
    
    public (Entity Entity, NetworkIdComponent NetId) CreateNetworkedEntity(
        ArchetypeId archetypeId,
        Entity scopeEntity,
        Action<EntityBuilder>? setComponents = null)
    {
        var netId = new NetworkIdComponent(_getPlayerId(), _nextNetworkedId++);
        var meta = new MetadataComponent(netId, archetypeId, netId.Creator);
        var entity = _store.CreateEntity(archetypeId, b =>
        {
            b.Add(meta);
            if (!scopeEntity.IsNull)
            {
                var scope = new ScopeComponent(scopeEntity);
                b.Add(scope);
            }
            // NOTE: This is added "temporarily" in order to mark the entity as not yet propagated over the network
            // Once the entity is propagated, this tag gets removed.
            b.AddTag<LocallyCreatedEntityTag>();
            setComponents?.Invoke(b);
        });
        return (entity, netId);
    }

    public Entity CreateRemoteNetworkedEntity(MetadataComponent meta)
    {
        return _store.CreateEntity(meta.Archetype, b => b.Add(meta));
    }

    public bool TryGetEntityByNetworkId(NetworkIdComponent netId, [NotNullWhen(true)] out Entity? entity)
    {
        // FIXME: Shouldn't this be cached?
        var ix = _store.ComponentIndex<MetadataComponent, NetworkIdComponent>();
        var matching = ix[netId];

        switch (matching.Count)
        {
            case 0:
                entity = null;
                return false;
            case 1:
                entity = matching[0];
                return true;
            default:
                _logger.LogError("Multiple entities found with NetworkIdComponent {NetworkId}. This should not happen.", netId);
                entity = null;
                return false;
        }
    }
}