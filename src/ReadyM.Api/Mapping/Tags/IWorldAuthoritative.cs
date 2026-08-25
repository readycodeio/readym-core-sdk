using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Tags;

/// <summary>
/// Marks a component the server authors on the world entity and projects onto every client, which applies
/// it to the game. The world entity has no client owner, so it is not gated on ownership.
/// </summary>
public interface IWorldAuthoritative : IMappingContext<Entity>
{
}
