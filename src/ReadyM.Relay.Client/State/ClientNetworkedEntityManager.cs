using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using ReadyM.Api.State;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Relay.Client.State;

public class ClientNetworkedEntityManager(
    ClientState state,
    NetworkedEntityManager netEntity) : IEntityManager
{
    public Entity CreateEntity(
        ArchetypeId archetypeId,
        Entity? scopeEntity,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null)
    {
        var (entity, _) = netEntity.CreateNetworkedEntity(archetypeId, scopeEntity, setComponents, ownerOverride);
        return entity;
    }

    public Entity CreateGlobalEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null)
        => CreateEntity(archetypeId, null, setComponents, ownerOverride);

    public Entity CreateAreaEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null)
    {
        if (!state.CurrentAreaEntity.HasValue)
            throw new InvalidOperationException("Attempted to create a networked entity in area but no area is set.");

        var scopeEntity = state.CurrentAreaEntity.Value;
        var (entity, _) = netEntity.CreateNetworkedEntity(archetypeId, scopeEntity, setComponents);
        return entity;
    }
    
    public Entity CreatePlayerEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null)
    {
        if (state.LocalPlayerEntity == null)
            throw new InvalidOperationException("Attempted to create a networked entity for player but no player entity is set.");
            
        var scopeEntity = state.LocalPlayerEntity.Value;
        var (entity, _) = netEntity.CreateNetworkedEntity(archetypeId, scopeEntity, setComponents);
        return entity;
    }

    public bool TryGetEntityByNetworkId(NetworkId netId, [NotNullWhen(true)] out Entity? entity)
        => netEntity.TryGetEntityByNetworkId(netId, out entity);
}