namespace ReadyM.Api.Multiplayer.Protocol;

/// <summary>
/// Denotes a reason for being disconnected from the server.
/// </summary>
public enum DisconnectedReason : byte
{
    /// <summary>
    /// Reason for disconnection was unknown.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Connection to the server timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Client disconnected voluntarily.
    /// </summary>
    ClientDisconnected,

    /// <summary>
    /// The client-side SDK mod version is incompatible with the server. 
    /// </summary>
    IncompatibleVersion,

    /// <summary>
    /// Connection ticket issued by the server has expired.
    /// </summary>
    ExpiredTicket,

    /// <summary>
    /// Player is already connected to the server in another session.
    /// </summary>
    AlreadyConnected,

    /// <summary>
    /// Server is full and does not allow new connections.
    /// </summary>
    ServerFull,

    /// <summary>
    /// Player was kicked from the server by an admin.
    /// </summary>
    Kicked,

    /// <summary>
    /// Player was banned from the server by an admin.
    /// </summary>
    Banned,

    /// <summary>
    /// The whole server was banned by ReadyM, so it refuses all players.
    /// </summary>
    ServerBanned,
}
