namespace ReadyM.SDK.Attributes;

/// <summary>
/// Indicates how a <see cref="ClientToClientsAttribute"/> message is relayed to other players in the game.
/// </summary>
/// <remarks>Its values are the wire protocol's, so generated code passes them through unchanged.</remarks>
public enum RelayMode : byte
{
    /// <summary>
    /// Sends the message to all players in the area of interest of the sender, other than the sender.
    /// </summary>
    AreaOfInterestOthers = 0,

    /// <summary>
    /// Sends the message to all players in the area of interest of the sender, including the sender.
    /// </summary>
    AreaOfInterestAll = 1,

    /// <summary>
    /// Sends the message to all players in the game, other than the sender.
    /// </summary>
    GlobalOthers = 2,

    /// <summary>
    /// Sends the message to all players in the game, including the sender.
    /// </summary>
    GlobalAll = 3,
}
