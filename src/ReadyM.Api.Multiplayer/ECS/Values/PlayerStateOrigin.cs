namespace ReadyM.Api.Multiplayer.ECS.Values;

/// <summary>
/// Where the state of a player's entities came from when they connected: their save on this server, or their game.
/// </summary>
public enum PlayerStateOrigin : byte
{
    /// <summary>This server has no save of the player: they are new here, and their game supplied the state.</summary>
    Game = 0,

    /// <summary>Loaded from the player's save on this server: they have played here before.</summary>
    Save = 1,
}
