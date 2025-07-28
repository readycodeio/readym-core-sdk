using System;
using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

public class ApplyDeltaJob<T>(NetworkedEntityManager netManager, Func<PlayerId> getPlayerId) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    public void Execute(NetDataReader reader)
    {
        var playerId = getPlayerId();
        
        // event code and component id are already read by the caller
        while (reader.TryGetNetworkId(out var netId))
        {
            if (playerId != PlayerId.Server && netId.Creator == playerId)
            {
                // do not apply deltas for entities created by the local player
                // this is a hack to avoid rubberbanding until we refactor the delta system
                default(T).SkipDelta(reader);
                continue;
            }

            if (!netManager.TryGetEntityByNetworkId(netId, out var entity))
            {
                if (netManager.IsNetworkEntityDestroyed(netId))
                {
                    // already dead, skip
                    default(T).SkipDelta(reader);
                    continue;
                }

                // it must be new
                // TODO: un-hardcode this after refactoring
                entity = netManager.CreateRemoteNetworkedEntity(new ArchetypeId(0), netId);
            }

            if (!entity.Value.HasComponent<T>())
            {
                entity.Value.AddComponent<T>();
            }

            ref var component = ref entity.Value.GetComponent<T>();
            component.ReadDelta(reader);
        }
    }
}