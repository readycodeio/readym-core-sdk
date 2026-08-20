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
    /// <param name="builder">The archetype builder with the configured components.</param>
    /// <returns>The identifier of the defined archetype.</returns>
    ArchetypeId RegisterArchetype(ArchetypeBuilder builder);

    /// <summary>
    /// Extends an existing entity archetype with additional components.
    /// </summary>
    /// <param name="archetypeId">The identifier of the archetype to modify.</param>
    /// <param name="callback">This callback will be invoked immediately to modify the existing registered archetype
    /// builder. It is NOT invoked on each call</param>
    void ModifyArchetype(ArchetypeId archetypeId, Action<ArchetypeBuilder> callback);

    void RegisterFilter(IArchetypeBuilderCallback filter);
}
