using System;
using System.Linq;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Extensions;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal class ApplyDeltaJob<T>(INetworkTime netTime, IChangeTrackingStore changeTrackingStore, INetworkedEntityManager netEntity, IPlayerIdProvider playerIdProvider, ILogger logger) : IJob<NetDataReader>
    where T : struct, INetworkedComponent
{
    private readonly bool _useSetComponent =
        typeof(T).GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIndexedComponent<>));

    [ThreadStatic]
    private static T _skipinstance;

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
                _skipinstance.ReadDelta(reader);
                continue;
            }

            var owner = entity.Value.GetComponent<MetadataComponent>().Owner;

            if (authoritativeSender.HasValue && owner != authoritativeSender.Value)
            {
                // Non-owner sender: consume the bytes to stay aligned, but do not apply/relay.
                logger.LogWarning(
                    "Dropping delta for {Component} entity {NetId}: sender {Sender} is not the owner {Owner}",
                    typeof(T).Name, netId, authoritativeSender.Value, owner);
                _skipinstance.ReadDelta(reader);
                continue;
            }

            var lastObservedServerTime = reader.GetUInt();
            var currentTime = netTime.GetCurrentTime();
            using var _ = ComponentWriteContext.EnterServerApplyDelta(currentTime, lastObservedServerTime, changeTrackingStore);

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
                    var component = entity.Value.GetComponent<T>();
                    component.ReadDeltaTracking(reader, entity.Value.Id);

                    if (playerId == owner)
                    {
                        // Owner-directed deltas are always server overrides; keep the API flag so the
                        // sync copies it to the game actor, and clear dirty so we don't echo it back.
                        component.MarkChangedFromApi();
                        component.ClearDirty();
                    }

                    entity.Value.Set(component);
                }
                else
                {
                    var component = default(T);
                    component.ReadDeltaTracking(reader, entity.Value.Id);

                    if (playerId == owner)
                    {
                        // Owner-directed deltas are always server overrides; keep the API flag so the
                        // sync copies it to the game actor, and clear dirty so we don't echo it back.
                        component.MarkChangedFromApi();
                        component.ClearDirty();
                    }

                    entity.Value.Add(component);
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
                component.ReadDelta(reader);

                if (playerId == owner)
                {
                    component.MarkChangedFromApi();
                    component.ClearDirty();
                }
            }
        }
    }
}
