using System;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Extensions;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal class ApplyDeltaJob<T>(INetworkedEntityManager netEntity, IPlayerIdProvider playerIdProvider) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    private readonly bool _useSetComponent = typeof(IForceSetComponent).IsAssignableFrom(typeof(T));

    [ThreadStatic]
    private static T _skipinstance;

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
                _skipinstance.ReadDelta(reader);
                continue;
            }

            var owner = entity.Value.GetComponent<MetadataComponent>().Owner;
            
            // entity exists, apply the delta
            // we assume entities are always created with the correct archetype
            if (_useSetComponent)
            {
                var component = default(T);
                component.ReadDelta(reader);

                if (playerId == owner)
                {
                    component.ClearDirty();
                }

                entity.Value.Set(component);
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