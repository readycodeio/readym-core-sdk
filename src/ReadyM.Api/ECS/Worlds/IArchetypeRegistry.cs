using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.ECS.Worlds;

public interface IArchetypeRegistry
{
    ArchetypeId RegisterArchetype(Action<EntityBuilderBase> constructor);
    void ModifyArchetype(ArchetypeId archetypeId, Action<EntityBuilderBase> constructor);
}