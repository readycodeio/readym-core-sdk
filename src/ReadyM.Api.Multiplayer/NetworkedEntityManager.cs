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
    public IEnumerable<NetDataWriter> WriteEcsDelta(int maxPacketSize)
    {
        var writer = new NetDataWriter();
        writer.Put((byte) SystemEvent.EcsUpdate);

        var query = _store.Query<NetworkIdComponent>();

        foreach (var entity in query.Entities)
        {
            var retried = false;
            var data = entity.Data;

            ref var netId = ref data.Get<NetworkIdComponent>();
            // ref var animation = ref data.Get<AnimationComponent>();
            // ref var health = ref data.Get<HpComponent>();
            // ref var monsterAnimation = ref data.Get<MonsterAnimationComponent>();
            // ref var nickname = ref data.Get<NicknameComponent>();
            // ref var team = ref data.Get<TeamComponent>();
            // ref var translation = ref data.Get<TranslationComponent>();
            // ref var tamer = ref data.Get<TamerComponent>();

            while (true)
            {
                var beforeApplyPosition = writer.Length;

                // var anyDirty = animation.IsDirty ||
                //                health.IsDirty ||
                //                monsterAnimation.IsDirty ||
                //                nickname.IsDirty ||
                //                team.IsDirty ||
                //                translation.IsDirty ||
                //                tamer.IsDirty;
                //
                // if (!anyDirty)
                //     yield break;
                //
                // writer.Put(netId);
                //
                // animation.WriteDelta(writer);
                // health.WriteDelta(writer);
                // monsterAnimation.WriteDelta(writer);
                // nickname.WriteDelta(writer);
                // team.WriteDelta(writer);
                // translation.WriteDelta(writer);
                // tamer.WriteDelta(writer);

                if (writer.Length > maxPacketSize)
                {
                    if (retried)
                    {
                        // if we retried and still failed, log an error
                        throw new Exception("Packet too large, unable to send");
                    }

                    // Rewind and send the partial packet
                    writer.SetPosition(beforeApplyPosition);
                    yield return writer;

                    // Start a new writer and retry
                    writer = new NetDataWriter();
                    writer.Put((byte) SystemEvent.EcsUpdate);
                    retried = true;

                    // Continue loop to retry
                    continue;
                }

                // animation.ClearDirty();
                // health.ClearDirty();
                // monsterAnimation.ClearDirty();
                // nickname.ClearDirty();
                // team.ClearDirty();
                // translation.ClearDirty();
                // tamer.ClearDirty();

                break;
            }
        }

        if (writer.Length > 1)
        {
            yield return writer;
        }
    }

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