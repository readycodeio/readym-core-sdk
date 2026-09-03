using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Players;

[Obsolete("PlayerApi will be merged into ServerEventsApi in the future. Please use ServerEventsApi instead.")]
public sealed class PlayerDisconnectedEvent
{
    public required PlayerId PlayerId { get; init; }
    public required Guid ReadyMId { get; init; }
}
