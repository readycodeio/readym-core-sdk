using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;

namespace ReadyM.Api.Multiplayer;

public sealed class NetworkedEntityManager : IDisposable
{
    private uint _nextNetworkedId;
    public event Action<NetworkIdComponent>? OnEntityDeleted;

    private readonly HashSet<NetworkIdComponent> _netIdTombstones = [];

    private readonly Store _store;
    private readonly Func<PlayerId> _getPeerId;

    public NetworkedEntityManager(Store store, Func<PlayerId> getPeerId)
    {
        _store = store;
        _getPeerId = getPeerId;
        _store.OnEntityDelete += HandleEntityDestroy;
    }

    public bool IsNetworkEntityDestroyed(NetworkIdComponent networkId)
    {
        return _netIdTombstones.Contains(networkId);
    }

    private void HandleEntityDestroy(EntityDelete evt)
    {
        if (evt.Entity.TryGetComponent<NetworkIdComponent>(out var netId))
        {
            _netIdTombstones.Add(netId);
            OnEntityDeleted?.Invoke(netId);
        }
    }

    public void Dispose()
    {
        _store.OnEntityDelete -= HandleEntityDestroy;
    }

    public (Entity Entity, NetworkIdComponent NetId) CreateNetworkedEntity(ArchetypeId archetypeId, Action<EntityBuilder>? setComponents = null)
    {
        var netId = new NetworkIdComponent(_getPeerId(), _nextNetworkedId++);
        var entity = _store.CreateEntity(archetypeId, b =>
        {
            b.Add(netId);
            setComponents?.Invoke(b);
        });
        return (entity, netId);
    }

    [Obsolete]
    public Entity CreateRemoteNetworkedEntity(ArchetypeId archetypeId, NetworkIdComponent netId)
    {
        return _store.CreateEntity(archetypeId, b => b.Add(netId));
    }

    internal Entity CreateRemoteNetworkedEntity(NetworkIdComponent netId)
    {
        return _store.CreateEntity(b => b.Add(netId));
    }

    public bool TryGetEntityByNetworkId(NetworkIdComponent netId, [NotNullWhen(true)] out Entity? entity)
    {
        var ix = _store.ComponentIndex<NetworkIdComponent, NetworkIdComponent>();
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
                // TODO: Log warning about multiple entities with the same NetworkIdComponent
                entity = null;
                return false;
        }
    }
}