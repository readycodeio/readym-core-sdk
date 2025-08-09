using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Relay.Client.State;

public class ClientNetworkedEntityState(NetworkedEntityManager netEntity, ClientState state, ILogger logger)
{
    public (Entity Entity, NetworkId NetId) CreateNetworkedGlobalEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null)
        => netEntity.CreateNetworkedEntity(archetypeId, null, setComponents);

    public (Entity Entity, NetworkId NetId) CreateNetworkedAreaEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null)
    {
        if (state.CurrentAreaEntity.HasValue)
        {
            var scopeEntity = state.CurrentAreaEntity.Value;
            return netEntity.CreateNetworkedEntity(archetypeId, scopeEntity, setComponents);
        }
        else
        {
            logger.LogError("Attempted to create a networked entity in area but no area is set.");
            return (default, default);
        }
    }
    
    public (Entity Entity, NetworkId NetId) CreateNetworkedPlayerEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null)
    {
        if (state.LocalPlayerEntity != null)
        {
            var scopeEntity = state.LocalPlayerEntity.Value;
            return netEntity.CreateNetworkedEntity(archetypeId, scopeEntity, setComponents);
        }
        else
        {
            logger.LogError("Attempted to create a networked entity for player but no player entity is set.");
            return (default, default);
        }
    }
    
    public bool TryGetEntityByNetworkId(NetworkId netId, [NotNullWhen(true)] out Entity? entity)
        => netEntity.TryGetEntityByNetworkId(netId, out entity);
}