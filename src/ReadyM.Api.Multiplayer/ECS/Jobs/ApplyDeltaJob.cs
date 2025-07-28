using System;
using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

public class ApplyDeltaJob<T>(NetworkedEntityManager netEntity, Func<PlayerId> getPlayerId) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    public void Execute(NetDataReader reader)
    {
        var playerId = getPlayerId();

        while (reader.TryGetNetworkId(out var netId))
        {
            if (playerId != PlayerId.Server && netId.Creator == playerId)
            {
                // do not apply deltas for entities created by the local player
                // this is a hack to avoid rubberbanding until we refactor the delta system
                default(T).SkipDelta(reader);
                continue;
            }

            if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            {
                // entity is dead or unknown, skip
                default(T).SkipDelta(reader);
                continue;
            }

            // entity exists, apply the delta
            // we assume entities are always created with the correct archetype

            ref var component = ref entity.Value.GetComponent<T>();
            component.ReadDelta(reader);
        }
    }
}