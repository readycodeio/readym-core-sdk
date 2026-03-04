using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Extensions;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

public class ApplyDeltaJob<T>(NetworkedEntityManager netEntity, IPlayerIdProvider playerIdProvider) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    private readonly bool _useSetComponent = typeof(IForceSetComponent).IsAssignableFrom(typeof(T));
    
    public void Execute(NetDataReader reader)
    {
        var playerId = playerIdProvider.PlayerId;
        if (playerId == null)
            return;

        while (reader.TryGetNetworkId(out var netId))
        {
            if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            {
                // entity is dead or unknown, skip
                default(T).SkipDelta(reader);
                continue;
            }

            var owner = entity.Value.GetComponent<MetadataComponent>().Owner;

            // if we are a client and the entity is owned by us, skip the delta
            if (playerId != PlayerId.Server && playerId == owner)
            {
                // NOTE: To make the server implementation easier, each client receives exactly the same delta message
                // This means that a client will receive deltas from the same entities that it SENDS deltas for.
                // In order to avoid ping-ponging delta messages back and forth, we skip deltas for entities that are 
                // owned by this client.
                default(T).SkipDelta(reader);
                continue;
            }

            // entity exists, apply the delta
            // we assume entities are always created with the correct archetype

            if (_useSetComponent)
            {
                var component = default(T);
                component.ReadDelta(reader);
                entity.Value.Set(component);

                if (playerId == owner)
                {
                    component.ClearDirty();
                }
            }
            else
            {
                ref var component = ref entity.Value.GetComponent<T>();
                component.ReadDelta(reader);

                if (playerId == owner)
                {
                    component.ClearDirty();
                }
            }
        }
    }
}