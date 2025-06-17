using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

public class ApplySnapshotJob<T>(NetworkedEntityManager netManager) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    public void Execute(NetDataReader reader)
    {
        var numEntities = reader.GetUInt();

        for (uint i = 0; i < numEntities; i++)
        {
            var netId = reader.Get<NetworkIdComponent>();
            
            if (!netManager.TryGetEntityByNetworkId(netId, out var entity))
            {
                if (netManager.IsNetworkEntityDestroyed(netId))
                {
                    // already dead, skip
                    default(T).SkipDelta(reader);
                    continue;
                }

                // it must be new
                entity = netManager.CreateRemoteNetworkedEntity(netId);
            }

            entity.Value.AddComponent(reader.Get<T>());
        }
    }
}