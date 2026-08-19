using System;
using System.Linq;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal class ApplySnapshotJob<T>(INetworkedEntityManager netEntity) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    private readonly bool _useSetComponent =
        typeof(T).GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIndexedComponent<>));

    [ThreadStatic]
    private static T _skipinstance;

    public void Execute(NetDataReader reader)
    {
        var entityCount = reader.GetUInt();

        for (uint i = 0; i < entityCount; i++)
        {
            var netId = reader.Get<NetworkId>();

            if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            {
                // snapshots are required to create entities, so if we don't have the entity, we skip the delta
                _skipinstance.Deserialize(reader);
                continue;
            }

            // STOP! DO NOT CHANGE this block without consulting the people maintaining networked components and native
            // components!
            // - Networked components have internal delta calculation that depends on in-place changes.
            // This doesn't apply here because CURRENTLY clients never send snapshots of anything to the server.
            // - Native components have allocations that need to be DISPOSED first, without that we leak memory.
            // YES, all fields of `comp` will be overwritten BUT deltas should still be correctly updated.
            if (_useSetComponent)
            {
                if (entity.Value.HasComponent<T>())
                {
                    ref var comp = ref entity.Value.GetComponent<T>();
                    comp.DeserializeTracking(reader, entity.Value.Id);
                    entity.Value.Set(comp);
                }
                else
                {
                    entity.Value.AddComponent(reader.Get<T>());
                }
            }
            else
            {
                if (entity.Value.HasComponent<T>())
                {
                    ref var comp = ref entity.Value.GetComponent<T>();
                    comp.DeserializeTracking(reader, entity.Value.Id);
                }
                else
                {
                    entity.Value.AddComponent(reader.Get<T>());
                }
            }
        }
    }
}
