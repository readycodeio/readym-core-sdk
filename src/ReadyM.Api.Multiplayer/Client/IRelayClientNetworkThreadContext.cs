using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.Client;

public interface IRelayClientNetworkThreadContext
{
    /// <summary>
    /// Whether the client is currently connected to the server - including having a valid `PlayerId` assigned. If
    /// the client disconnects as a result of a network error, this will be set to `false`, while `IsRunning` will
    /// remain `true` until `Stop()` is called.
    /// </summary>
    bool Connected { get; }
    
    PlayerId? PlayerId { get; }
    ReadOnlyList<PlayerId> AllPlayers { get; }
    
    AreaId? CurrentArea { get; }
    ReadOnlyList<PlayerId> AreaPlayers { get; }
}