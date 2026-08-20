using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.State;

internal interface IClientEntityManager
{
    Entity CreateEntity(
        ArchetypeId archetypeId,
        Entity? scopeEntity,
        PlayerId? ownerOverride = null);

    Entity CreateGlobalEntity(
        ArchetypeId archetypeId,
        PlayerId? ownerOverride = null);

    Entity CreateAreaEntity(
        ArchetypeId archetypeId,
        PlayerId? ownerOverride = null);

    Entity CreateCellEntity(
        CellId cellId,
        ArchetypeId archetypeId,
        PlayerId? ownerOverride = null);

    Entity CreatePlayerEntity(ArchetypeId archetypeId);
}
