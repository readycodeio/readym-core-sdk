namespace ReadyM.Api.Multiplayer.Networking;

/// <summary>Which LiteNetLib callback a queued event came from.</summary>
internal enum NetEventKind : byte
{
    ConnectionRequest,
    Connected,
    Disconnected,
    Receive,
    LatencyUpdate,
}
