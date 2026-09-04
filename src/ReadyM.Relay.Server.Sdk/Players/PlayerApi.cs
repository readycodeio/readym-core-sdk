using System.Runtime.InteropServices;
using ReadyM.Api.Idents;
using ReadyM.Api.Interop;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Players;

// FIXME: ~jk: Maybe I'm mistaken but this seems to duplicate some of the functionality of the wider ServerEventsApi
/// <summary>
/// Provides access to player events and actions on the server.
/// </summary>
public class PlayerApi
{
    private readonly KickPlayerDelegate _kickPlayer;
    private readonly PinnedDelegateStore _pinnedDelegateStore = new();
    private readonly INetworkTime _netTime;
    private readonly IChangeTrackingStore _changeTracking;
    private readonly GetReadyMIdDelegate _getReadyMId;

    internal PlayerApi(PlayerApiPointers pointers, INetworkTime netTime, IChangeTrackingStore changeTracking)
    {
        _netTime = netTime;
        _changeTracking = changeTracking;
        _kickPlayer = Marshal.GetDelegateForFunctionPointer<KickPlayerDelegate>(pointers.KickPlayer);
        _getReadyMId = Marshal.GetDelegateForFunctionPointer<GetReadyMIdDelegate>(pointers.GetReadyMId);

        PlayerEventHandlerDelegate bridge = OnPlayerEvent;
        _pinnedDelegateStore.PinDelegate(bridge);
        Marshal.GetDelegateForFunctionPointer<AddPlayerEventHandlerDelegate>(pointers.AddPlayerEventHandler)(bridge);
    }

    /// <summary>
    /// Fired once the player has finished the handshake and their ECS entity exists.
    /// </summary>
    [Obsolete("This API is being deprecated in favor of the more general ServerEventsApi.")]

    public event Action<PlayerConnectedEvent>? OnPlayerConnected;

    /// <summary>Fired when the player leaves, whichever way they left.</summary>
    [Obsolete("This API is being deprecated in favor of the more general ServerEventsApi.")]
    public event Action<PlayerDisconnectedEvent>? OnPlayerDisconnected;

    /// <summary>
    /// Kicks the player from the server. This is a hard disconnect, and the player will not be able to reconnect until they restart the game.
    /// </summary>
    /// <param name="player">The player to kick.</param>
    public void Kick(PlayerId player) => _kickPlayer(player);

    /// <summary>
    /// Get the id ReadyM assigned to this player's account, or null if this server has not seen them since it started.
    /// Global to the platform: the same player carries the same id every time, it survives reconnects
    /// and server restarts. <see cref="PlayerId"/> does none of that, so key anything you persist on this instead.
    /// </summary>
    public Guid? GetReadyMId(PlayerId player)
    {
        var readyMId = _getReadyMId(player);
        return readyMId != Guid.Empty ? readyMId : null;
    }

    private void OnPlayerEvent(PlayerEventData data)
    {
        // NOTE: We are on the ECS thread already (PlayerApi forwards ServerState)
        using var _ = ComponentWriteContext.EnterServerAuthoring(_netTime.GetCurrentTime(), _changeTracking);

        switch (data.Kind)
        {
            case PlayerEventKind.Connected:
                OnPlayerConnected?.Invoke(new PlayerConnectedEvent
                {
                    PlayerId = data.PlayerId,
                    ReadyMId = data.ReadyMId,
                });
                break;

            case PlayerEventKind.Disconnected:
                OnPlayerDisconnected?.Invoke(new PlayerDisconnectedEvent
                {
                    PlayerId = data.PlayerId,
                    ReadyMId = data.ReadyMId,
                });
                break;
        }
    }
}
