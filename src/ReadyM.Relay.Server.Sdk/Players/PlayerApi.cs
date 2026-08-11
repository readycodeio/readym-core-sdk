using System.Runtime.InteropServices;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Interop;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Players;

public class PlayerApi
{
    private readonly KickPlayerDelegate _kickPlayer;
    private readonly PinnedDelegateStore _pinnedDelegateStore = new();
    private readonly PlayerEventHandlerDelegate _bridge;
    private readonly GetReadyMIdDelegate _getReadyMId;

    internal unsafe PlayerApi(PlayerApiPointers pointers)
    {
        _kickPlayer = Marshal.GetDelegateForFunctionPointer<KickPlayerDelegate>(pointers.KickPlayer);
        _getReadyMId = Marshal.GetDelegateForFunctionPointer<GetReadyMIdDelegate>(pointers.GetReadyMId);

        _bridge = OnPlayerEvent;
        _pinnedDelegateStore.PinDelegate(_bridge);
        Marshal.GetDelegateForFunctionPointer<AddPlayerEventHandlerDelegate>(pointers.AddPlayerEventHandler)(_bridge);
    }

    /// <summary>
    /// Fired once the player has finished the handshake and their ECS entity exists.
    /// </summary>
    public event Action<PlayerConnectedEvent>? OnPlayerConnected;

    /// <summary>Fired when the player leaves, whichever way they left.</summary>
    public event Action<PlayerDisconnectedEvent>? OnPlayerDisconnected;

    public void Kick(PlayerId player) => _kickPlayer(player);

    /// <summary>
    /// Get the id ReadyM assigned to this player's account, or null if this server has not seen them since it started.
    /// Global to the platform: the same player carries the same id every time, it survives reconnects
    /// and server restarts. <see cref="PlayerId"/> does none of that, so key anything you persist on this instead.
    /// </summary>
    public unsafe Guid? GetReadyMId(PlayerId player)
    {
        Guid readyMId;
        return _getReadyMId(player, &readyMId) != 0 ? readyMId : null;
    }

    private unsafe void OnPlayerEvent(byte* data, int size)
    {
        var reader = new NetDataReader(new Span<byte>(data, size).ToArray());

        using var _ = ComponentWriteContext.EnterServerAuthoring();

        switch ((PlayerEventKind)reader.GetByte())
        {
            case PlayerEventKind.Connected:
            {
                var playerId = reader.Get<PlayerId>();
                var readyMId = new Guid(reader.GetBytesWithLength());

                OnPlayerConnected?.Invoke(new PlayerConnectedEvent
                {
                    PlayerId = playerId,
                    ReadyMId = readyMId,
                });
                break;
            }
            case PlayerEventKind.Disconnected:
            {
                var playerId = reader.Get<PlayerId>();
                var readyMId = new Guid(reader.GetBytesWithLength());

                OnPlayerDisconnected?.Invoke(new PlayerDisconnectedEvent
                {
                    PlayerId = playerId,
                    ReadyMId = readyMId,
                });
                break;
            }
        }
    }
}
