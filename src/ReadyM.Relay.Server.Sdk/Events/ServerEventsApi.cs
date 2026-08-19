using System.Runtime.InteropServices;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Interop;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Events;

public sealed class ServerEventsApi : IDisposable
{
    public event Action<PlayerId, Entity>? OnPlayerConnected;
    public event Action<PlayerId, Entity, DisconnectReason>? OnPlayerDisconnected;
    public event Action<AreaId, Entity>? OnAreaCreated;
    public event Action<AreaId, Entity>? OnAreaDeleted;
    public event Action<PlayerId, AreaId>? OnPlayerJoinedArea;
    public event Action<PlayerId, AreaId>? OnPlayerLeftArea;
    public event Action<AreaId, CellId, Entity>? OnCellCreated;
    public event Action<AreaId, CellId, Entity>? OnCellDeleted;
    public event Action<PlayerId, AreaId, CellId>? OnPlayerActivatedCell;
    public event Action<PlayerId, AreaId, CellId>? OnPlayerDeactivatedCell;

    /// <summary>
    /// Raised once, when the world entity exists. A mod's Init runs before that, so this is the earliest
    /// point at which world components can be written.
    /// </summary>
    public event Action<Entity>? OnWorldEntityCreated;

    private readonly UnsubscribeServerEventsDelegate _unsubscribe;
    private readonly ServerEventHandlerDelegate _dispatch;
    private readonly PinnedDelegateStore _pinnedDelegates = new();
    private readonly EcsApi _ecs;
    private readonly INetworkTime _netTime;
    private readonly IChangeTrackingStore _changeTracking;
    private readonly ILogger _logger;

    internal ServerEventsApi(
        ServerEventsPointers pointers,
        EcsApi ecs,
        INetworkTime netTime,
        IChangeTrackingStore changeTracking,
        ILogger logger)
    {
        if (pointers.Subscribe == IntPtr.Zero || pointers.Unsubscribe == IntPtr.Zero)
        {
            throw new ArgumentException("The host provided no server events bridge", nameof(pointers));
        }

        _ecs = ecs;
        _netTime = netTime;
        _changeTracking = changeTracking;
        _logger = logger;
        _unsubscribe = Marshal.GetDelegateForFunctionPointer<UnsubscribeServerEventsDelegate>(pointers.Unsubscribe);

        _dispatch = Dispatch;
        _pinnedDelegates.PinDelegate(_dispatch);

        var subscribe = Marshal.GetDelegateForFunctionPointer<SubscribeServerEventsDelegate>(pointers.Subscribe);
        subscribe(_dispatch);
    }

    public void Dispose()
    {
        _unsubscribe(_dispatch);
        _pinnedDelegates.Dispose();
    }

    private void Dispatch(ServerEventKind kind, ServerEventPayload payload)
    {
        try
        {
            // NOTE: We are on the ECS thread already (ServerEventsApi forwards ServerState)
            using var _ = ComponentWriteContext.EnterServerAuthoring(_netTime.GetCurrentTime(), _changeTracking);
            Raise(kind, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A server event handler threw while handling {Kind}", kind);
        }
    }

    private void Raise(ServerEventKind kind, ServerEventPayload payload)
    {
        var entity = _ecs.EntityFrom(payload.EntityId);

        switch (kind)
        {
            case ServerEventKind.PlayerConnected:
                OnPlayerConnected?.Invoke(payload.Player, entity);
                break;
            case ServerEventKind.PlayerDisconnected:
                OnPlayerDisconnected?.Invoke(payload.Player, entity, payload.Reason);
                break;
            case ServerEventKind.AreaCreated:
                OnAreaCreated?.Invoke(payload.Area, entity);
                break;
            case ServerEventKind.AreaDeleted:
                OnAreaDeleted?.Invoke(payload.Area, entity);
                break;
            case ServerEventKind.PlayerJoinedArea:
                OnPlayerJoinedArea?.Invoke(payload.Player, payload.Area);
                break;
            case ServerEventKind.PlayerLeftArea:
                OnPlayerLeftArea?.Invoke(payload.Player, payload.Area);
                break;
            case ServerEventKind.CellCreated:
                OnCellCreated?.Invoke(payload.Area, payload.Cell, entity);
                break;
            case ServerEventKind.CellDeleted:
                OnCellDeleted?.Invoke(payload.Area, payload.Cell, entity);
                break;
            case ServerEventKind.PlayerActivatedCell:
                OnPlayerActivatedCell?.Invoke(payload.Player, payload.Area, payload.Cell);
                break;
            case ServerEventKind.PlayerDeactivatedCell:
                OnPlayerDeactivatedCell?.Invoke(payload.Player, payload.Area, payload.Cell);
                break;
            case ServerEventKind.WorldEntityCreated:
                OnWorldEntityCreated?.Invoke(entity);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled server event kind");
        }
    }
}
