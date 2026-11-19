using ReadyM.Api.Idents;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Dimensions;

/// <summary>
/// The server's dimensions: parallel copies of the world, or of some areas of it. A dimension is data a mod passes:
/// queries, lookups and player lists take one, and those that take none see every dimension.
/// </summary>
public interface IDimensions
{
    /// <summary>Whether a dimension is live.</summary>
    bool Exists(DimensionId id);

    /// <summary>The live dimension with this name.</summary>
    bool TryFind(string name, out DimensionId id);

    /// <summary>
    /// Creates a dimension. Its areas and cells are provided as players join them; a persistent one's saved areas and
    /// cells are loaded back from its save, found by its name.
    /// </summary>
    /// <exception cref="InvalidOperationException">The name is taken, or the parent is not live.</exception>
    /// <exception cref="ArgumentException">A persistent dimension has no name.</exception>
    DimensionId Create(DimensionOptions options);

    /// <summary>
    /// Destroys a dimension: its players go to its parent, and it is deleted once none of them is in one of its areas.
    /// </summary>
    /// <exception cref="InvalidOperationException">It is the main dimension, or it has child dimensions.</exception>
    /// <exception cref="KeyNotFoundException">It is not live.</exception>
    void Destroy(DimensionId id);

    /// <summary>
    /// The dimension <see cref="SetPlayerDimension"/> gave the player: where their next area join resolves from.
    /// </summary>
    DimensionId GetPlayerDimension(PlayerId player);

    /// <summary>
    /// The dimension the player is in now: the dimension of the area they stand in, else <see cref="GetPlayerDimension"/>.
    /// What <c>RpcContext.Dimension</c> holds for a sender.
    /// </summary>
    DimensionId GetCurrentDimension(PlayerId player);

    /// <summary>The entity's dimension; <see cref="DimensionId.Shared"/> for one in no dimension.</summary>
    DimensionId GetEntityDimension(EntityHandle entity);

    /// <summary>
    /// Whether the dimension is the entity's origin: the one it was created in, or loaded into when its origin was not
    /// saved (a temporary dimension's). A persistent origin matches that dimension again once a mod creates it.
    /// False for an entity that has not been in a dimension.
    /// </summary>
    bool IsEntityOriginDimension(EntityHandle entity, DimensionId dimension);

    /// <summary>
    /// Moves a player to a dimension. Their client reloads only when the area they stand in changes dimension;
    /// otherwise the move applies at their next area join.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The dimension is not live, or the player is not connected.</exception>
    /// <exception cref="InvalidOperationException">The dimension is being destroyed.</exception>
    DimensionSwitchMode SetPlayerDimension(PlayerId player, DimensionId id);
}
