using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.ECS.Worlds;

/// <summary>
/// Allows registering and modifying entity archetypes in the ECS.
/// </summary>
public interface IArchetypeRegistry
{
    /// <summary>
    /// Registers a new entity archetype with the configured components.
    /// </summary>
    /// <param name="build">An action for adding components.</param>
    /// <returns>The identifier of the defined archetype.</returns>
    ArchetypeId RegisterArchetype(Action<EntityBuilderBase> build);
    
    /// <summary>
    /// Extends an existing entity archetype with additional components.
    /// </summary>
    /// <param name="archetypeId">The identifier of the archetype to modify.</param>
    /// <param name="build">A action for adding components.</param>
    void ModifyArchetype(ArchetypeId archetypeId, Action<EntityBuilderBase> build);
}