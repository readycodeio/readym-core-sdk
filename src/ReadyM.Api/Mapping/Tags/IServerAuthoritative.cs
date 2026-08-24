using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.Tags;

/// <summary>
/// Marks a component the server authors and projects onto its owner, who applies it to the game
/// and never writes it back. The game holds no counterpart to read from.
/// </summary>
public interface IServerAuthoritative : IMappingContext<Entity>
{
    // empty
}
