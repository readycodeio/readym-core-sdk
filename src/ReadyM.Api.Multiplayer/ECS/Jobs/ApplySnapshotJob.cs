using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

public class ApplySnapshotJob<T>(NetworkedEntityManager netEntity) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    public void Execute(NetDataReader reader)
    {
        var numEntities = reader.GetUInt();

        for (uint i = 0; i < numEntities; i++)
        {
            var netId = reader.Get<NetworkId>();

            if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            {
                // snapshots are required to create entities, so if we don't have the entity, we skip the delta
                default(T).Deserialize(reader);
                continue;
            }

            entity.Value.AddComponent(reader.Get<T>());
        }
    }
}