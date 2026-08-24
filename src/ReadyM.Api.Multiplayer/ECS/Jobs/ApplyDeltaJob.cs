using System;
using System.Linq;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Extensions;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal class ApplyDeltaJob<T>(
    INetworkedEntityManager netEntity,
    IPlayerIdProvider playerIdProvider,
    ILogger logger)
    : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    private readonly bool _useSetComponent =
        typeof(T).GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIndexedComponent<>));

    [ThreadStatic]
    private static T _skipInstance;

    public void Execute(NetDataReader reader)
    {
        var playerId = playerIdProvider.PlayerId;
        if (playerId == null)
            return;

        // Server-only: the sending client. Unset on the client (deltas there are trusted relays).
        var authoritativeSender = DeltaApplyContext.AuthoritativeSender;

        while (reader.TryGetNetworkId(out var netId))
        {
            if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            {
                // entity is dead or unknown, skip
                _skipInstance.ReadDelta(reader);
                continue;
            }

            var owner = entity.Value.GetComponent<MetadataComponent>().Owner;

            if (authoritativeSender.HasValue && owner != authoritativeSender.Value)
            {
                // Non-owner sender: consume the bytes to stay aligned, but do not apply/relay.
                logger.LogWarning(
                    "Dropping delta for {Component} entity {NetId}: sender {Sender} is not the owner {Owner}",
                    typeof(T).Name, netId, authoritativeSender.Value, owner);
                _skipInstance.ReadDelta(reader);
                continue;
            }

            // STOP! DO NOT CHANGE this block without consulting the people maintaining networked components and native
            // components!
            // - Networked components have internal delta calculation that depends on in-place changes.
            // This doesn't apply here because CURRENTLY clients never send snapshots of anything to the server.
            // - Native components have allocations that need to be DISPOSED first, without that we leak memory.
            // YES, all fields of `comp` will be overwritten BUT deltas should still be correctly updated.

            // entity exists, apply the delta
            // we assume entities are always created with the correct archetype
            if (_useSetComponent)
            {
                if (entity.Value.HasComponent<T>())
                {
                    var comp = entity.Value.GetComponent<T>();
                    comp.ReadDeltaTracking(reader, entity.Value.Id);

                    if (playerId == owner)
                    {
                        // NOTE: Client never receives deltas for its owned entities UNLESS they come from the server
                        // BECAUSE of the server-authoritative from API changes. If that's the case we "forward" that
                        // change to the client. So on the client side we no longer want to have dirtyMask set (because
                        // this change already came from the server, so there's no need to propagate it back to the
                        // server), and we want to set "from API" flag because we want to treat that situation as if
                        // something got changed "from API" on the client side.
                        comp.MarkChangedFromApi();
                        comp.ClearDirty();
                    }

                    entity.Value.Set(comp);
                }
                else
                {
                    var comp = default(T);
                    comp.ReadDeltaTracking(reader, entity.Value.Id);

                    if (playerId == owner)
                    {
                        // NOTE: See analogous comment above on the other branch.
                        comp.MarkChangedFromApi();
                        comp.ClearDirty();
                    }

                    entity.Value.Add(comp);
                }
            }
            else
            {
                if (!entity.Value.HasComponent<T>())
                {
                    logger.LogWarning(
                        "Dropping delta for {Component} entity {NetId}: no such component on the target entity",
                        typeof(T).Name, netId);
                    return;
                }

                ref var component = ref entity.Value.GetComponent<T>();
                component.ReadDeltaTracking(reader, entity.Value.Id);

                if (playerId == owner)
                {
                    // NOTE: See analogous comment above on the other branch.
                    component.MarkChangedFromApi();
                    component.ClearDirty();
                }
            }
        }
    }
}
