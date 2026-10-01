using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

internal interface INetworkedEntityManager
{
    event Action<NetworkId, Entity>? OnEntityDelete;

    /// <summary>
    /// Raised when a live entity's <see cref="NetworkId"/> stops being valid.
    /// </summary>
    event Action<NetworkId, Entity>? OnNetworkEntityRetired;
    void SetNextNetworkedId(uint nextId);
    bool IsNetworkEntityDeleted(NetworkId netId);
    (Entity Entity, NetworkId NetId) CreateNetworkedEntity(
        ArchetypeId archetypeId,
        Entity? scopeEntity,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null);
    Entity CreateRemoteNetworkedEntity(MetadataComponent meta, Entity? scopeEntity);

    /// <summary>
    /// Moves a networked entity into <paramref name="scopeEntity"/>, or to global when it is null.
    /// </summary>
    void MoveToScope(Entity entity, Entity? scopeEntity);
    bool TryGetEntityByNetworkId(NetworkId netId, [NotNullWhen(true)] out Entity? entity);
    void DeleteEntitiesInScope(Entity scopeEntity, bool skipSync, bool deleteScopeEntity);
    void DeleteAllNetworkedEntities(bool skipSync);
    bool TryDeleteEntity(int entityId);
}
