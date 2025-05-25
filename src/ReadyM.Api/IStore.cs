using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace ReadyM.Api;

internal interface IStore
{
    SystemRoot SystemRoot { get; }
    ArchetypeId RegisterArchetype(Action<EntityBuilder> populateComponents);

    /// <summary>
    /// Creates a new entity with the specified archetype.
    /// </summary>
    /// <param name="archetypeId"></param>
    /// <returns></returns>
    Entity CreateEntity(ArchetypeId archetypeId);
}