using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Tags;

/// <summary>
/// Marks a component the server authors on the world entity and projects onto every client, who applies
/// it to the game and never writes it back. Unlike <see cref="IServerAuthoritative"/> it is not gated on
/// ownership, because the world entity has no client owner.
/// </summary>
public interface IWorldAuthoritative : IMappingContext<Entity>
{
    // empty
}
