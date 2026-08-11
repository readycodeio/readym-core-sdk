using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Players;

public sealed class PlayerConnectedEvent
{
    public required PlayerId PlayerId { get; init; }
    public required Guid ReadyMId { get; init; }
}
