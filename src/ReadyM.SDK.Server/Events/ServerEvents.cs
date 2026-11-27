using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Interop;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Events;

/// <summary>Hands the host's server events to mods, with the entity each one is about as its shape.</summary>
internal sealed class ServerEvents : IServerEvents, IDisposable
{
    public event Action<Player, Guid>? OnPlayerConnected;
    public event Action<Player, Guid, DisconnectReason>? OnPlayerDisconnected;
    public event Action<Area, DimensionId>? OnAreaCreated;
    public event Action<Area, DimensionId>? OnAreaDeleted;
    public event Action<PlayerId, AreaId, DimensionId>? OnPlayerJoinedArea;
    public event Action<PlayerId, AreaId, DimensionId>? OnPlayerLeftArea;
    public event Action<Cell, DimensionId>? OnCellCreated;
    public event Action<Cell, DimensionId>? OnCellDeleted;
    public event Action<PlayerId, FullCellId, DimensionId>? OnPlayerActivatedCell;
    public event Action<PlayerId, FullCellId, DimensionId>? OnPlayerDeactivatedCell;
    public event Action<World>? OnWorldCreated;
    public event Action<Player>? OnPlayerWorldReloadRequested;
    public event Action<DimensionId>? OnDimensionEmptied;
    public event Action<PlayerId, DimensionId, DimensionId>? OnPlayerDimensionChanged;
    public event Action<EntityHandle, DimensionId, DimensionId>? OnEntityDimensionChanged;

    private readonly UnsubscribeServerEventsDelegate _unsubscribe;
    private readonly ServerEventHandlerDelegate _dispatch;
    private readonly PinnedDelegateStore _pinnedDelegates = new();
    private readonly IEntityApi _entities;
    private readonly INetworkTime _netTime;
    private readonly IChangeTrackingStore _changeTracking;
    private readonly ILogger _logger;
    private bool _disposed;

    internal ServerEvents(
        ServerEventsPointers pointers,
        IEntityApi entities,
        INetworkTime netTime,
        IChangeTrackingStore changeTracking,
        ILogger logger)
    {
        if (pointers.Subscribe == IntPtr.Zero || pointers.Unsubscribe == IntPtr.Zero)
            throw new ArgumentException("The host provided no server events bridge", nameof(pointers));

        _entities = entities;
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
        // Registered as both this class and IServerEvents, so the container may dispose it twice
        if (_disposed)
            return;

        _disposed = true;
        _unsubscribe(_dispatch);
        _pinnedDelegates.Dispose();
    }

    private void Dispatch(ServerEventKind kind, ServerEventPayload payload)
    {
        try
        {
            // The host raises these on the ECS thread
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
        var dimension = payload.Dimension;

        switch (kind)
        {
            case ServerEventKind.PlayerConnected:
                OnPlayerConnected?.Invoke(Shape<Player>(payload.Entity), payload.ReadyMId);
                break;
            case ServerEventKind.PlayerDisconnected:
                OnPlayerDisconnected?.Invoke(Shape<Player>(payload.Entity), payload.ReadyMId, payload.Reason);
                break;
            case ServerEventKind.AreaCreated:
                OnAreaCreated?.Invoke(Shape<Area>(payload.Entity), dimension);
                break;
            case ServerEventKind.AreaDeleted:
                OnAreaDeleted?.Invoke(Shape<Area>(payload.Entity), dimension);
                break;
            case ServerEventKind.PlayerJoinedArea:
                OnPlayerJoinedArea?.Invoke(payload.Player, payload.Area, dimension);
                break;
            case ServerEventKind.PlayerLeftArea:
                OnPlayerLeftArea?.Invoke(payload.Player, payload.Area, dimension);
                break;
            case ServerEventKind.CellCreated:
                OnCellCreated?.Invoke(Shape<Cell>(payload.Entity), dimension);
                break;
            case ServerEventKind.CellDeleted:
                OnCellDeleted?.Invoke(Shape<Cell>(payload.Entity), dimension);
                break;
            case ServerEventKind.PlayerActivatedCell:
                OnPlayerActivatedCell?.Invoke(payload.Player, new FullCellId(dimension, payload.Area, payload.Cell), dimension);
                break;
            case ServerEventKind.PlayerDeactivatedCell:
                OnPlayerDeactivatedCell?.Invoke(payload.Player, new FullCellId(dimension, payload.Area, payload.Cell), dimension);
                break;
            case ServerEventKind.WorldEntityCreated:
                OnWorldCreated?.Invoke(Shape<World>(payload.Entity));
                break;
            case ServerEventKind.PlayerWorldReloadRequested:
                OnPlayerWorldReloadRequested?.Invoke(Shape<Player>(payload.Entity));
                break;
            case ServerEventKind.DimensionEmptied:
                OnDimensionEmptied?.Invoke(dimension);
                break;
            case ServerEventKind.PlayerDimensionChanged:
                OnPlayerDimensionChanged?.Invoke(payload.Player, payload.PreviousDimension, dimension);
                break;
            case ServerEventKind.EntityDimensionChanged:
                OnEntityDimensionChanged?.Invoke(new EntityHandle(payload.Entity, _entities), payload.PreviousDimension, dimension);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled server event kind");
        }
    }

    private T Shape<T>(RawEntity entity) where T : struct, IArchetypeQueryable
        => new() { Handle = new EntityHandle(entity, _entities) };
}
