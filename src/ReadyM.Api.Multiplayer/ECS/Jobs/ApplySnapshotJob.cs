using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;

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
                // TODO: un-hardcode archetype ID after refactoring
                entity = netManager.CreateRemoteNetworkedEntity(new ArchetypeId(0), netId);
            }

            entity.Value.AddComponent(reader.Get<T>());
        }
    }
}