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
internal class PlayerApi : IPlayers
{
    private readonly KickPlayerDelegate _kickPlayer;
    private readonly PinnedDelegateStore _pinnedDelegateStore = new();
    private readonly INetworkTime _netTime;
    private readonly IChangeTrackingStore _changeTracking;
    private readonly GetReadyMIdDelegate _getReadyMId;
    private readonly RotateCellMastersDelegate _rotateCellMasters;
    private readonly GetAreaPlayersDelegate _getAreaPlayers;
    private readonly GetCellPlayersDelegate _getCellPlayers;

    internal PlayerApi(PlayerApiPointers pointers, INetworkTime netTime, IChangeTrackingStore changeTracking)
    {
        _netTime = netTime;
        _changeTracking = changeTracking;
        _kickPlayer = Marshal.GetDelegateForFunctionPointer<KickPlayerDelegate>(pointers.KickPlayer);
        _getReadyMId = Marshal.GetDelegateForFunctionPointer<GetReadyMIdDelegate>(pointers.GetReadyMId);
        _rotateCellMasters = Marshal.GetDelegateForFunctionPointer<RotateCellMastersDelegate>(pointers.RotateCellMasters);
        _getAreaPlayers = Marshal.GetDelegateForFunctionPointer<GetAreaPlayersDelegate>(pointers.GetAreaPlayers);
        _getCellPlayers = Marshal.GetDelegateForFunctionPointer<GetCellPlayersDelegate>(pointers.GetCellPlayers);

        PlayerEventHandlerDelegate bridge = OnPlayerEvent;
        _pinnedDelegateStore.PinDelegate(bridge);
        Marshal.GetDelegateForFunctionPointer<AddPlayerEventHandlerDelegate>(pointers.AddPlayerEventHandler)(bridge);
    }

    /// <summary>
    /// Fired once the player has finished the handshake and their ECS entity exists.
    /// </summary>
    [Obsolete("Use IServerEvents instead")]

    public event Action<PlayerConnectedEvent>? OnPlayerConnected;

    /// <summary>Fired when the player leaves, whichever way they left.</summary>
    [Obsolete("Use IServerEvents instead")]
    public event Action<PlayerDisconnectedEvent>? OnPlayerDisconnected;

    /// <summary>
    /// Kicks the player from the server. This is a hard disconnect, and the player will not be able to reconnect until they restart the game.
    /// </summary>
    /// <param name="player">The player to kick.</param>
    public void Kick(PlayerId player) => _kickPlayer(player);

    /// <summary>
    /// Test tool: every cell the requester has active gets the next player in it as master client,
    /// and every entity in that cell that allows ownership transfer follows to the new master.
    /// </summary>
    public void RotateCellMasters(PlayerId requester) => _rotateCellMasters(requester);

    /// <inheritdoc />
    public unsafe List<PlayerId> InArea(AreaId area) 
        => Collect((b, c) => _getAreaPlayers(area, b, c));

    /// <inheritdoc />
    public unsafe List<PlayerId> InCell(FullCellId cell) 
        => Collect((b, c) => _getCellPlayers(cell, b, c));

    /// <summary>A pointer cannot be a generic argument, so the ask has a type of its own.</summary>
    private unsafe delegate int Ask(PlayerId* buffer, int capacity);

    /// <summary>
    /// Asks for the players, growing the buffer once if the first ask did not fit. The host answers with the
    /// count whether or not it wrote anything, so a second ask with that much room always works.
    /// </summary>
    private static unsafe List<PlayerId> Collect(Ask ask)
    {
        List<PlayerId> into = [];

        const int room = 64;
        var small = stackalloc PlayerId[room];
        var found = ask(small, room);

        if (found <= room)
        {
            for (var i = 0; i < found; i++)
                into.Add(small[i]);

            return into;
        }

        var large = new PlayerId[found];
        fixed (PlayerId* buffer = large)
        {
            found = ask(buffer, large.Length);
        }

        for (var i = 0; i < found && i < large.Length; i++)
            into.Add(large[i]);
        
        return into;
    }

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
