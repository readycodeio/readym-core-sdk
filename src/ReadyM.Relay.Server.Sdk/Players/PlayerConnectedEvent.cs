using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Players;

public sealed class PlayerConnectedEvent
{
    public required PlayerId PlayerId { get; init; }

    /// <summary>
    /// The player's ReadyM account id. Stable across reconnects and server restarts
    /// </summary>
    public required Guid UserGuid { get; init; }
}
