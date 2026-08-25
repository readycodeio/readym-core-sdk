using System;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;

namespace ReadyM.Api.State;

internal interface IClientEntityManager
{
    Entity CreateEntity(
        ArchetypeId archetypeId,
        Entity? scopeEntity,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null);

    Entity CreateGlobalEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null);

    Entity CreateAreaEntity(
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null);

    Entity CreateCellEntity(
        CellId cellId,
        ArchetypeId archetypeId,
        Action<EntityBuilder>? setComponents = null,
        PlayerId? ownerOverride = null);

    Entity CreatePlayerEntity(ArchetypeId archetypeId);
}
