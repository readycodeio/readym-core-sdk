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
    public void Execute(NetDataReader reader)
    {
        var entityCount = reader.GetUInt();

        for (uint i = 0; i < entityCount; i++)
        {
            var netId = reader.Get<NetworkId>();

            if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            {
                // snapshots are required to create entities, so if we don't have the entity, we skip the delta
                default(T).Deserialize(reader);
                continue;
            }

            if (entity.Value.HasComponent<T>())
            {
                var comp = entity.Value.GetComponent<T>();
                comp.Deserialize(reader);
                entity.Value.Set(comp);
            }
            else
            {
                entity.Value.AddComponent(reader.Get<T>());
            }
        }
    }
}