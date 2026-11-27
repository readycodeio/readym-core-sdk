using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.SDK.Archetypes.Core;

namespace ReadyM.SDK.Client.Events;

/// <summary>This client's connection to the server: connecting, losing it, and getting it back. Raised on the game thread.</summary>
public interface IClientEvents
{
    /// <summary>Fires when this client has connected and its player exists, and again after each reconnect.</summary>
    event Action<Player>? OnConnected;

    /// <summary>
    /// Fires when the connection ends, and again when reconnecting stops for good; the flag says whether the client
    /// will try to connect again.
    /// </summary>
    event Action<DisconnectedReason, bool>? OnDisconnected;

    /// <summary>Fires before each attempt to get a lost connection back, with the attempt counted from 1.</summary>
    event Action<int>? OnReconnecting;
}
