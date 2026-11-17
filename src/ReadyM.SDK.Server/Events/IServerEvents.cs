using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes.Core;

namespace ReadyM.SDK.Server.Events;

/// <summary>
/// The server's events: players connecting and moving, and areas and cells coming and going. An area or cell event
/// carries the dimension it happened in.
/// </summary>
public interface IServerEvents
{
    /// <summary>Fires when a player has connected and their player entity exists, with their ReadyM id.</summary>
    event Action<Player, Guid>? OnPlayerConnected;

    /// <summary>Fires when a player has left or dropped, after they left their area.</summary>
    event Action<Player, Guid, DisconnectReason>? OnPlayerDisconnected;

    /// <summary>Fires when an area comes into use, as the first player enters it.</summary>
    event Action<Area, DimensionId>? OnAreaCreated;

    /// <summary>Fires when the last player has left an area, unless the server keeps it while empty.</summary>
    event Action<Area, DimensionId>? OnAreaDeleted;

    /// <summary>Fires when any player enters an area, the first one included.</summary>
    event Action<PlayerId, AreaId, DimensionId>? OnPlayerJoinedArea;

    /// <summary>Fires when any player leaves an area.</summary>
    event Action<PlayerId, AreaId, DimensionId>? OnPlayerLeftArea;

    /// <summary>Fires when a cell comes into use, as the first player loads it.</summary>
    event Action<Cell, DimensionId>? OnCellCreated;

    /// <summary>Fires when the last player has unloaded a cell, unless the server keeps it while empty.</summary>
    event Action<Cell, DimensionId>? OnCellDeleted;

    /// <summary>Fires when any player loads a cell, the first one included.</summary>
    event Action<PlayerId, FullCellId, DimensionId>? OnPlayerActivatedCell;

    /// <summary>Fires when any player unloads a cell.</summary>
    event Action<PlayerId, FullCellId, DimensionId>? OnPlayerDeactivatedCell;

    /// <summary>
    /// Raised once, when the world entity exists. A mod's Init runs before that, so this is the earliest point at which
    /// world values can be written.
    /// </summary>
    event Action<World>? OnWorldCreated;

    /// <summary>
    /// Fires when a player in an area has moved to another dimension and world reload is requested.
    /// </summary>
    event Action<Player>? OnPlayerWorldReloadRequested;

    /// <summary>
    /// Fires once when a dimension becomes empty: no player in it or in one of its areas, and no child dimension.
    /// </summary>
    event Action<DimensionId>? OnDimensionEmptied;

    /// <summary>Fires when a player's dimension changes, by a switch or a join that moved them up: player, from, to.</summary>
    event Action<PlayerId, DimensionId, DimensionId>? OnPlayerDimensionChanged;
}
