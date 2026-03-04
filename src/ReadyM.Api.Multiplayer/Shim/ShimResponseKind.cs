namespace ReadyM.Api.Multiplayer.Shim;

public enum ShimResponseKind
{
    None = 0,
    Connected = 2,
    Disconnected = 4,
    OtherPlayerConnected = 5,
    OtherPlayerDisconnected = 6,
    JoinedArea = 8,
    LeftArea = 10,
    OtherPlayerJoinedArea = 11,
    OtherPlayerLeftArea = 12,
    PingUpdated = 13,
    AnyBuiltInMessage = 14,
    AnyServerMessage = 15,
    AnyClientMessage = 16,
}
