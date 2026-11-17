namespace ReadyM.SDK.Server.Dimensions;

/// <summary>What moving a player to a dimension does.</summary>
public enum DimensionSwitchMode : byte
{
    /// <summary>The player is in that dimension already.</summary>
    None,

    /// <summary>The area the player stands in changes dimension, so their client reloads its world.</summary>
    Reload,

    /// <summary>No reload: the player's next area join resolves from the new dimension.</summary>
    NextJoin,
}
