using System;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Extensions;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal class ApplyDeltaJob<T>(INetworkedEntityManager netEntity, IPlayerIdProvider playerIdProvider, ILogger logger) : IJob<NetDataReader>
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

        // On the server this is the client that sent the batch; a client may only change components
        // on entities it owns. Unset on the client (deltas there are trusted server relays).
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
                // Hostile/buggy external input: a client trying to change an entity it does not own.
                // Consume the bytes to stay aligned, but do not apply (so it is never relayed either).
                logger.LogWarning(
                    "Dropping delta for {Component} entity {NetId}: sender {Sender} is not the owner {Owner}",
                    typeof(T).Name, netId, authoritativeSender.Value, owner);
                _skipinstance.ReadDelta(reader);
                continue;
            }

            // entity exists, apply the delta
            // we assume entities are always created with the correct archetype
            if (_useSetComponent)
            {
                var component = default(T);
                component.ReadDelta(reader);

                if (playerId == owner)
                {
                    // The server only ever sends the owner deltas for its own entity when it is
                    // authoritatively overriding the owner's state (ChangedFromApi). Preserve that
                    // signal so the client-side sync copies the value to the game actor and does not
                    // let the local game state clobber it. Clear dirty so we do not echo it back.
                    component.MarkChangedFromApi();
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
                    component.MarkChangedFromApi();
                    component.ClearDirty();
                }
            }
        }
    }
}