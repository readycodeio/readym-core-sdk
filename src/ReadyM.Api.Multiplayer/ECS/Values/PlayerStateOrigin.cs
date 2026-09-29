namespace ReadyM.Api.Multiplayer.ECS.Values;

/// <summary>
/// Where the state of a player entity came from when the server created it.
/// </summary>
public enum PlayerStateOrigin : byte
{
    /// <summary>No saved data: the owning client fills the entity from the game.</summary>
    Game = 0,

    /// <summary>Built from the player's save slot: the owning client pushes it into the game.</summary>
    Save = 1,
}
