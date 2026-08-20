using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.State;

namespace ReadyM.Relay.Client.State;

internal class ClientNetworkedEntityState(
    ClientState state,
    INetworkedEntityManager netEntity) : IClientEntityManager
{
    public Entity CreateEntity(
        ArchetypeId archetypeId,
        Entity? scopeEntity,
        PlayerId? ownerOverride = null)
    {
        var (entity, _) = netEntity.CreateNetworkedEntity(archetypeId, scopeEntity, setComponents: null, ownerOverride);
        return entity;
    }

    public Entity CreateGlobalEntity(
        ArchetypeId archetypeId,
        PlayerId? ownerOverride = null)
        => CreateEntity(archetypeId, null, ownerOverride);

    public Entity CreateAreaEntity(
        ArchetypeId archetypeId,
        PlayerId? ownerOverride = null)
    {
        if (!state.CurrentAreaEntity.HasValue)
            throw new InvalidOperationException("Attempted to create a networked entity in area but no area is set.");

        var scopeEntity = state.CurrentAreaEntity.Value;
        var (entity, _) = netEntity.CreateNetworkedEntity(archetypeId, scopeEntity);
        return entity;
    }

    public Entity CreateCellEntity(
        CellId cellId,
        ArchetypeId archetypeId,
        PlayerId? ownerOverride = null)
    {
        var cellEntry = state.GetActiveCellEntry(cellId);
        if (!cellEntry.HasValue)
            throw new InvalidOperationException($"Attempted to create a networked entity in cell {cellId} but that cell is not active.");

        var scopeEntity = cellEntry.Value.CellEntity;
        var (entity, _) = netEntity.CreateNetworkedEntity(archetypeId, scopeEntity);
        return entity;
    }

    public Entity CreatePlayerEntity(ArchetypeId archetypeId)
    {
        if (state.LocalPlayerEntity == null)
            throw new InvalidOperationException("Attempted to create a networked entity for player but no player entity is set.");

        var scopeEntity = state.LocalPlayerEntity.Value;
        var (entity, _) = netEntity.CreateNetworkedEntity(archetypeId, scopeEntity);
        return entity;
    }
}
