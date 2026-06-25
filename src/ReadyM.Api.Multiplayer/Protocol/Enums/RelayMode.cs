using System;
using System.ComponentModel;

namespace ReadyM.Api.Multiplayer.Protocol.Enums;

/// <summary>
/// Indicates how an RPC message should be relayed to other players in the game.
/// </summary>
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

    /// <summary>
    /// Sends the message to the owner of an entity, possibly back to the sender.
    /// </summary>
    /// <remarks>
    /// Not part of stable API.
    /// </remarks>
    [Obsolete("Not part of stable API.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    EntityOwner = 4,

    /// <summary>
    /// Sends the message to a specific list of players.
    /// </summary>
    /// <remarks>
    /// Not part of stable API.
    /// </remarks>
    [Obsolete("Not part of stable API.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    Peers = 5,
}