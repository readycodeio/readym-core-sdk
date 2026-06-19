namespace ReadyM.Api.Multiplayer.Shim;

internal enum ShimRequestKind
{
    None = 0,
    RequestedConnect = 1,
    RequestedDisconnect = 2,
    RequestedJoinArea = 3,
    RequestedLeaveArea = 4,
    SentBuiltInMessage = 5,
    SentServerRpcMessage = 6,
    SentClientRpcMessage = 7,
    RequestedSetActiveCells = 8,
}