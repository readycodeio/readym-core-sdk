using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.SDK.Client.Relay;

/// <summary>The game's answer to whether it connects again after its connection ended; the game owns reconnecting.</summary>
internal interface IReconnectPolicy
{
    /// <summary>Whether the game will try to connect again after a connection that ended for this reason.</summary>
    bool WillReconnectAfter(DisconnectedReason reason);
}
