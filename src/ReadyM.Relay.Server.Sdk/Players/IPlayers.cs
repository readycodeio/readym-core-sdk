using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Players;

/// <summary>What the server can do with a connected player, beyond their entities.</summary>
public interface IPlayers
{
    /// <summary>
    /// Kicks the player from the server. This is a hard disconnect, and the player will not be able to reconnect until
    /// they restart the game.
    /// </summary>
    /// <param name="player">The player to kick.</param>
    void Kick(PlayerId player);

    /// <summary>
    /// Get the id ReadyM assigned to this player's account, or null if this server has not seen them since it started.
    /// Global to the platform: the same player carries the same id every time, it survives reconnects and server
    /// restarts. <see cref="PlayerId"/> does none of that, so key anything you persist on this instead.
    /// </summary>
    Guid? GetReadyMId(PlayerId player);

    /// <summary>The players in an area, in every dimension.</summary>
    List<PlayerId> InArea(AreaId area);

    /// <summary>The players in an area of one dimension.</summary>
    List<PlayerId> InArea(AreaId area, DimensionId dimension);

    /// <summary>The players who have a cell active, in every dimension.</summary>
    List<PlayerId> InCell(FullCellId cell);

    /// <summary>The players who have a cell of one dimension active.</summary>
    List<PlayerId> InCell(FullCellId cell, DimensionId dimension);
}
