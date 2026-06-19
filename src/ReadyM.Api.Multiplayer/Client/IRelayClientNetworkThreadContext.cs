using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.Api.Multiplayer.Client;

internal interface IRelayClientNetworkThreadContext
{
    /// <summary>
    /// Whether the client is currently connected to the server - including having a valid `PlayerId` assigned. If
    /// the client disconnects as a result of a network error, this will be set to `false`, while `IsRunning` will
    /// remain `true` until `Stop()` is called.
    /// </summary>
    bool IsConnected { get; }
    DisconnectedReason LastDisconnectedReason { get; }
    
    PlayerId? PlayerId { get; }
    ReadOnlyList<PlayerId> AllPlayers { get; }
    
    AreaId? CurrentAreaId { get; }
    ReadOnlyList<CellId> ActiveCells { get; }
    ReadOnlyList <PlayerId> AreaPlayers { get; }
}