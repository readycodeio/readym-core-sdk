using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Relay.Common.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

public class ApplyDeltaJob<T>(NetworkedEntityManager netManager) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    public void Execute(NetDataReader reader)
    {
        // event code and component id are already read by the caller
        while (reader.TryGetNetworkId(out var netId))
        {
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
                entity.Value.AddComponent<T>();
            }

            ref var component = ref entity.Value.GetComponent<T>();
            component.ReadDelta(reader);
        }
    }
}