using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

public sealed class NetworkedEntityManager : INetworkedEntityManager, IDisposable
{
    private class NetworkedArchetypeConfiguration(NetworkedEntityManager manager, ArchetypeId archetypeId) : INetworkedArchetypeConfiguration
    {
        public INetworkedArchetypeConfiguration MarkSynced<T>() where T : struct, INetworkedComponent
        {
            if (manager._networkedComponents.TryGetValue(archetypeId, out var components))
            {
                components.Add(typeof(T));
            }
            else
            {
                manager._networkedComponents[archetypeId] = [typeof(T)];
            }

            return this;
        }
    }

    private uint _nextNetworkedId;
    public event Action<NetworkIdComponent>? onEntityDestroyed;

    private readonly HashSet<NetworkIdComponent> _netIdTombstones = [];

    private readonly Store _store;

    internal short PeerId { get; set; }

    private readonly Dictionary<ArchetypeId, List<Type>> _networkedComponents = new();


    public NetworkedEntityManager(Store store, short peerId)
    {
        _store = store;
        _store.OnEntityDelete += HandleEntityDestroy;
        PeerId = peerId;
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
            onEntityDestroyed?.Invoke(netId);
        }
    }

    public void Dispose()
    {
        _store.OnEntityDelete -= HandleEntityDestroy;
    }

    public void ConfigureArchetype(ArchetypeId archetypeId, Action<INetworkedArchetypeConfiguration> builder)
    {
        var configuration = new NetworkedArchetypeConfiguration(this, archetypeId);
        builder.Invoke(configuration);
    }

    public (Entity Entity, NetworkIdComponent NetId) CreateNetworkedEntity(ArchetypeId archetypeId)
    {
        var netId = new NetworkIdComponent(PeerId, _nextNetworkedId++);
        var entity = _store.CreateEntity(archetypeId);
        entity.AddComponent(netId);
        return (entity, netId);
    }
    
    public Entity CreateRemoteNetworkedEntity(ArchetypeId archetypeId, NetworkIdComponent netId)
    {
        var entity = _store.CreateEntity(archetypeId);
        entity.AddComponent(netId);
        return entity;
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