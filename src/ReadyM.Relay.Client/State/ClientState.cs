using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.Relay.Client.State;

internal class ClientState : IDisposable
{
    private class ProcessPendingEventsSystem(ClientState owner) : BaseSystem
    {
        protected override void OnUpdateGroup()
        {
            owner.ProcessPendingEvents();
        }
    }

    private enum PendingEventKind
    {
        Connected,
        Disconnected,
        JoinedArea,
        LeftArea,
        OtherPlayerCreated,
        OtherPlayerDeleted,
        OtherPlayerInsideArea,
        OtherPlayerOutsideArea,
        SetActiveCells
    }

    private struct PendingEvent
    {
        public bool Invalidated;
        public PendingEventKind Kind;
        public AreaId AreaId;
        public PlayerId PlayerId;
        public bool IsNotify;
        public DisconnectedReason disconnectedReason;
        public CellId[]? CellIds;
    }

    public struct AreaEntry
    {
        public AreaId AreaId { get; internal set; }
        public Entity AreaEntity { get; internal set; }
        public NetworkId AreaNetworkId { get; internal set; }

        public List<PlayerId> AreaPlayers { get; internal set; }
    }

    public struct CellEntry
    {
        public CellId CellId { get; internal set; }
        public Entity CellEntity { get; internal set; }
        public NetworkId CellNetworkId { get; internal set; }

        /// <summary>Players for whom this cell is considered active.</summary>
        public List<PlayerId> CellPlayers { get; internal set; }
    }

    public struct PlayerEntry
    {
        public PlayerId PlayerId { get; internal set; }
        public Entity PlayerEntity { get; internal set; }
        public NetworkId PlayerNetworkId { get; internal set; }
        public AreaId? CurrentAreaId { get; internal set; }
    }

    private readonly Store _world;
    private readonly INetworkedEntityManager _netEntity;
    private readonly IRelayClient _relayClient;
    private readonly ILogger _logger;

    private readonly List<PlayerId> _allPlayers = [];
    private readonly Dictionary<PlayerId, PlayerEntry> _playerEntries = new();

    // NOTE: For performance. The same as _pendingPlayerEntries[_localPlayerId]
    private PlayerEntry? _localPlayerEntry;

    private AreaEntry? _currentAreaEntry;

    private readonly List<CellEntry> _currentCellEntries = new();

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    private readonly object _lock = new();

    private readonly List<PendingEvent> _pendingEvents = [];

    private readonly ProcessPendingEventsSystem _system;

    public bool IsConnected
        => _localPlayerEntry != null;

    public PlayerId? LocalPlayerId
        => _localPlayerEntry?.PlayerId;

    public PlayerEntry? LocalPlayerEntry
        => _localPlayerEntry;

    public Entity? LocalPlayerEntity
        => _localPlayerEntry?.PlayerEntity;

    public BaseSystem System
        => _system;

    public Api.Helpers.ReadOnlyList<PlayerId> AllPlayers => new(_allPlayers);
    public Api.Helpers.ReadOnlyList<PlayerId> OtherPlayers => new([.. _allPlayers.Where(p => p != LocalPlayerId)]);
    public System.Collections.ObjectModel.ReadOnlyDictionary<PlayerId, PlayerEntry> PlayerEntries => new(_playerEntries);
    public Api.Helpers.ReadOnlyList<PlayerId> AreaPlayers => (_currentAreaEntry?.AreaPlayers).NullableWrapReadOnly();
    public Api.Helpers.ReadOnlyList<PlayerId> OtherAreaPlayers => (_currentAreaEntry?.AreaPlayers.Where(p => p != LocalPlayerId).ToList()).NullableWrapReadOnly();

    public bool JoinedArea
        => _currentAreaEntry is { } value && value.AreaId != AreaId.Invalid;

    public AreaId? CurrentAreaId
        => _currentAreaEntry?.AreaId;

    public Api.Helpers.ReadOnlyList<CellId> ActiveCells
        => _currentCellEntries.Select(entry => entry.CellId).ToList().WrapReadOnly();

    public Api.Helpers.ReadOnlyList<CellEntry> ActiveCellEntries
        => _currentCellEntries.WrapReadOnly();

    public CellEntry? GetActiveCellEntry(CellId cellId)
        => _currentCellEntries.FirstOrDefault(e => e.CellId == cellId) is var entry && entry.CellId == cellId ? entry : (CellEntry?)null;

    private bool IsLocalCellMaster(Entity cellEntity)
        => cellEntity != default
           && cellEntity.HasComponent<CellScopeComponent>()
           && LocalPlayerId is { } local
           && cellEntity.GetComponent<CellScopeComponent>().MasterClient == local;

    public AreaEntry? CurrentAreaEntry
        => _currentAreaEntry;

    public Entity? CurrentAreaEntity
        => _currentAreaEntry?.AreaEntity;

    public event Action<PlayerId, Entity>? OnConnected;
    public event Action<PlayerId, Entity?, DisconnectedReason>? OnDisconnected;
    public event Action<PlayerId, Entity, OtherPlayerCreatedReason>? OnOtherPlayerCreated;
    public event Action<PlayerId, Entity, OtherPlayerDeletedReason>? OnOtherPlayerDeleted;

    public event Action<AreaId, Entity>? OnJoinedArea;
    public event Action<AreaId, Entity>? OnLeftArea;
    /// <summary>
    /// Fired when a cell is activated for a player.
    /// FullCellId along with the Entity represents the cell, boolean indicates if the local player is the master of that cell.
    /// </summary>
    public event Action<FullCellId, Entity, bool>? OnActivatedCell;
    /// <summary>
    /// Fired when a cell is deactivated for a player.
    /// FullCellId along with the Entity represents the cell, boolean indicates if the local player was the master of that cell.
    /// </summary>
    public event Action<FullCellId, Entity, bool>? OnDeactivatedCell;
    public event Action<PlayerId, AreaId, OtherPlayerInsideAreaReason>? OnOtherPlayerInsideArea;
    public event Action<PlayerId, AreaId, OtherPlayerOutsideAreaReason>? OnOtherPlayerOutsideArea;

    public ArchetypeId AreaArchetype { get; }
    public ArchetypeId PlayerArchetype { get; }
    public ArchetypeId CellArchetype { get; }

    public ClientState(
        Store world,
        INetworkedEntityManager netEntity,
        IRelayClient relayClient,
        DefaultAreaArchetypeRegistration areaArchetype,
        DefaultPlayerArchetypeRegistration playerArchetype,
        DefaultCellArchetypeRegistration cellArchetype,
        ILogger logger)
    {
        _world = world;
        _netEntity = netEntity;
        _relayClient = relayClient;
        _logger = logger;

        AreaArchetype = areaArchetype.AreaArchetype;
        PlayerArchetype = playerArchetype.PlayerArchetype;
        CellArchetype = cellArchetype.CellArchetype;

        _system = new ProcessPendingEventsSystem(this);

        _relayClient.OnConnected += OnConnectedHandler;
        _relayClient.OnDisconnected += OnDisconnectedHandler;
        _relayClient.OnJoinedArea += OnJoinedAreaHandler;
        _relayClient.OnLeftArea += OnLeftAreaHandler;
        _relayClient.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        _relayClient.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        _relayClient.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        _relayClient.OnActiveCellsSet += OnActiveCellsSetHandler;
    }

    public void Dispose()
    {
        _relayClient.OnActiveCellsSet -= OnActiveCellsSetHandler;
        _relayClient.OnOtherPlayerLeftArea -= OnOtherPlayerLeftAreaHandler;
        _relayClient.OnOtherPlayerJoinedArea -= OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnOtherPlayerDisconnected -= OnOtherPlayerDisconnectedHandler;
        _relayClient.OnOtherPlayerConnected -= OnOtherPlayerConnectedHandler;
        _relayClient.OnLeftArea -= OnLeftAreaHandler;
        _relayClient.OnJoinedArea -= OnJoinedAreaHandler;
        _relayClient.OnDisconnected -= OnDisconnectedHandler;
        _relayClient.OnConnected -= OnConnectedHandler;
    }

    private void OnConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId, uint nextId)
    {
        _netEntity.SetNextNetworkedId(nextId);

        lock (_lock)
        {
            _pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.Connected,
                PlayerId = playerId,
            });
            foreach (var otherPlayerId in context.AllPlayers)
            {
                if (otherPlayerId == playerId)
                    continue;
                _pendingEvents.Add(new PendingEvent()
                {
                    Kind = PendingEventKind.OtherPlayerCreated,
                    PlayerId = otherPlayerId,
                    IsNotify = true,
                });
            }
        }
    }

    private void OnDisconnectedHandler(IRelayClientNetworkThreadContext context)
    {
        lock (_lock)
        {
            _pendingEvents.Add(new PendingEvent
            {
                Kind = PendingEventKind.Disconnected,
                PlayerId = context.PlayerId ?? PlayerId.Invalid,
                disconnectedReason = context.LastDisconnectedReason,
            });
        }
    }

    private void OnJoinedAreaHandler(IRelayClientNetworkThreadContext context, AreaId areaId)
    {
        lock (_lock)
        {
            _pendingEvents.Add(new PendingEvent
            {
                Kind = PendingEventKind.JoinedArea,
                AreaId = areaId,
                PlayerId = context.PlayerId!.Value,
            });

            foreach (var otherPlayerId in context.AreaPlayers)
            {
                if (otherPlayerId == context.PlayerId)
                    continue;
                _pendingEvents.Add(new PendingEvent()
                {
                    Kind = PendingEventKind.OtherPlayerInsideArea,
                    AreaId = areaId,
                    PlayerId = otherPlayerId,
                    IsNotify = true,
                });
            }
        }
    }

    private void OnLeftAreaHandler(IRelayClientNetworkThreadContext context)
    {
        lock (_lock)
        {
            var areaId = context.CurrentAreaId;
            if (areaId == null)
            {
                _logger.LogError("LeftArea event received, but no current area. This should not happen.");
                return;
            }

            var playerId = context.PlayerId;
            if (playerId == null)
            {
                _logger.LogError("LeftArea event received, but no player ID. This should not happen.");
                return;
            }

            _pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.LeftArea,
                AreaId = areaId.Value,
                PlayerId = playerId.Value,
            });
        }
    }

    private void OnOtherPlayerConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        lock (_lock)
        {
            _pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.OtherPlayerCreated,
                PlayerId = playerId,
            });
        }
    }

    private void OnOtherPlayerDisconnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        lock (_lock)
        {
            _pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.OtherPlayerDeleted,
                PlayerId = playerId,
            });
        }
    }

    private void OnActiveCellsSetHandler(IRelayClientNetworkThreadContext context)
    {
        lock (_lock)
        {
            var cellIds = context.ActiveCells;
            var ids = new CellId[cellIds.Count];
            for (var i = 0; i < cellIds.Count; i++)
                ids[i] = cellIds[i];

            _pendingEvents.Add(new PendingEvent
            {
                Kind = PendingEventKind.SetActiveCells,
                PlayerId = context.PlayerId!.Value,
                CellIds = ids,
            });
        }
    }

    private void OnOtherPlayerJoinedAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        lock (_lock)
        {
            _pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.OtherPlayerInsideArea,
                AreaId = context.CurrentAreaId!.Value,
                PlayerId = playerId,
            });
        }
    }

    private void OnOtherPlayerLeftAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        lock (_lock)
        {
            var areaId = context.CurrentAreaId;
            if (areaId == null)
            {
                _logger.LogError("OtherPlayerLeftArea event received, but no current area. This should not happen.");
                return;
            }

            _pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.OtherPlayerOutsideArea,
                AreaId = areaId.Value,
                PlayerId = playerId,
            });
        }
    }

    /// <summary>
    /// Invalidates all pending events that are no longer relevant.
    /// For example if a player disconnects and reconnects, the <see cref="PendingEventKind.Disconnected"/> will be invalidated,
    /// because what matters to us is the final state.
    /// </summary>
    private void PrunePendingEvents()
    {
        void InvalidateRange(int fromIndex, int toIndex, Func<PendingEvent, bool>? predicate = null)
        {
            for (var i = fromIndex; i <= toIndex; i++)
            {
                var p = _pendingEvents[i];
                p.Invalidated = predicate?.Invoke(p) ?? true;
                _pendingEvents[i] = p;
            }
        }

        void InvalidateOne(int index)
        {
            var p = _pendingEvents[index];
            p.Invalidated = true;
            _pendingEvents[index] = p;
        }

        bool IsAreaEvent(PendingEvent p)
            => p.Kind switch
            {
                PendingEventKind.Connected
                    or PendingEventKind.Disconnected
                    or PendingEventKind.OtherPlayerCreated
                    or PendingEventKind.OtherPlayerDeleted => false,
                PendingEventKind.JoinedArea
                    or PendingEventKind.SetActiveCells
                    or PendingEventKind.LeftArea
                    or PendingEventKind.OtherPlayerInsideArea
                    or PendingEventKind.OtherPlayerOutsideArea => true,
                _ => throw new ArgumentOutOfRangeException()
            };

        int? lastConnectedIndex = null;
        var playerId = _localPlayerEntry?.PlayerId;

        int? lastJoinedAreaIndex = null;
        var areaId = _currentAreaEntry?.AreaId;
        int? lastSetCellsIndex = null;
        var activeCells = _currentCellEntries.Select(entry =>entry.CellId).ToList();

        Dictionary<PlayerId, int> connectedPlayers = new();
        Dictionary<PlayerId, int> areaPlayers = new();

        for (var i = 0; i < _pendingEvents.Count; i++)
        {
            var pendingEvent = _pendingEvents[i];

            if (pendingEvent.Invalidated)
                continue;

            switch (pendingEvent.Kind)
            {
                case PendingEventKind.Connected:
                {
                    if (playerId == pendingEvent.PlayerId)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (lastConnectedIndex != null)
                    {
                        // NOTE: Assume the previous playerId should be assumed disconnected. Remove all events
                        // from the previous playerId, but keep the current event.
                        InvalidateRange(lastConnectedIndex.Value, i - 1);
                    }

                    lastConnectedIndex = i;
                    playerId = pendingEvent.PlayerId;
                    break;
                }
                case PendingEventKind.Disconnected:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (lastConnectedIndex != null)
                    {
                        // NOTE: Remove all events from the current playerId, including the current event.
                        InvalidateRange(lastConnectedIndex.Value, i);
                        pendingEvent.Invalidated = true;
                    }

                    lastConnectedIndex = null;
                    playerId = null;

                    lastJoinedAreaIndex = null;
                    areaId = null;
                    lastSetCellsIndex = null;
                    activeCells = null;

                    connectedPlayers.Clear();
                    areaPlayers.Clear();
                    break;
                }
                case PendingEventKind.JoinedArea:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (areaId == pendingEvent.AreaId)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (lastJoinedAreaIndex != null)
                    {
                        // NOTE: Assume the previous areaId should be assumed left. Remove all events
                        // from the previous areaId, but keep the current event.
                        InvalidateRange(lastJoinedAreaIndex.Value, i - 1, IsAreaEvent);
                    }

                    lastJoinedAreaIndex = i;
                    areaId = pendingEvent.AreaId;
                    lastSetCellsIndex = null;
                    activeCells = null;
                    break;
                }
                case PendingEventKind.LeftArea:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (lastJoinedAreaIndex != null)
                    {
                        // NOTE: Remove all events from the current areaId, including the current event.
                        InvalidateRange(lastJoinedAreaIndex.Value, i, IsAreaEvent);
                        pendingEvent.Invalidated = true;
                    }

                    lastJoinedAreaIndex = null;
                    areaId = null;
                    areaPlayers.Clear();
                    lastSetCellsIndex = null;
                    activeCells = null;
                    break;
                }
                case PendingEventKind.OtherPlayerCreated:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (connectedPlayers.TryGetValue(pendingEvent.PlayerId, out _))
                    {
                        // NOTE: If the player is already connected, remove the current duplicate event.
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    connectedPlayers.Add(pendingEvent.PlayerId, i);
                    break;
                }
                case PendingEventKind.OtherPlayerDeleted:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (connectedPlayers.TryGetValue(pendingEvent.PlayerId, out var otherPlayerConnectedIndex))
                    {
                        // NOTE: Remove the corresponding connected event
                        InvalidateOne(otherPlayerConnectedIndex);
                        pendingEvent.Invalidated = true;
                    }

                    connectedPlayers.Remove(pendingEvent.PlayerId);
                    areaPlayers.Remove(pendingEvent.PlayerId);
                    break;
                }
                case PendingEventKind.OtherPlayerInsideArea:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (areaId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (areaPlayers.TryGetValue(pendingEvent.PlayerId, out _))
                    {
                        // NOTE: If the player already joined, remove the current duplicate event.
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    areaPlayers.Add(pendingEvent.PlayerId, i);
                    break;
                }
                case PendingEventKind.OtherPlayerOutsideArea:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (areaId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (areaPlayers.TryGetValue(pendingEvent.PlayerId, out var otherPlayerJoinedIndex))
                    {
                        InvalidateOne(otherPlayerJoinedIndex);
                        pendingEvent.Invalidated = true;
                    }

                    areaPlayers.Remove(pendingEvent.PlayerId);
                    break;
                }
                case PendingEventKind.SetActiveCells:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (areaId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if ((activeCells == null || activeCells.Count <= 0) && (pendingEvent.CellIds == null || pendingEvent.CellIds.Length <= 0))
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    //Check if the set of active cells has actually changed
                    if (activeCells != null && pendingEvent.CellIds != null && activeCells.Count == pendingEvent.CellIds.Length)
                    {
                        var cellsChanged = false;
                        for (int j = 0; j < activeCells.Count; j++)
                        {
                            if (!pendingEvent.CellIds.Contains(activeCells[j]))
                            {
                                cellsChanged = true;
                                break;
                            }
                        }

                        if (!cellsChanged)
                        {
                            pendingEvent.Invalidated = true;
                            break;
                        }
                    }

                    if (lastSetCellsIndex != null)
                        InvalidateOne(lastSetCellsIndex.Value);

                    lastSetCellsIndex = i;
                    activeCells = pendingEvent.CellIds?.ToList();

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _pendingEvents[i] = pendingEvent;
        }
    }

    private void ProcessPendingEvents()
    {
        var removeAndContinue = false;

        while (true)
        {
            PendingEvent pendingEvent;

            lock (_lock)
            {
                if (removeAndContinue)
                    _pendingEvents.RemoveAt(0);

                PrunePendingEvents();

                if (_pendingEvents.Count == 0)
                    break;

                pendingEvent = _pendingEvents[0];
            }

            if (pendingEvent.Invalidated)
            {
                removeAndContinue = true;
                continue;
            }

            removeAndContinue = ProcessPendingEvent(pendingEvent);

            if (!removeAndContinue)
                return;
        }
    }

    private bool ProcessPendingEvent(PendingEvent pendingEvent)
    {
        switch (pendingEvent.Kind)
        {
            case PendingEventKind.Connected:
            {
                if (_localPlayerEntry != null)
                {
                    _logger.LogError("Connected event received, but local player entry already exists. This should not happen.");
                    _localPlayerEntry = null;
                    // break; skipped on purpose
                }

                if (_currentCellEntries.Count != 0)
                {
                    _logger.LogError("Connected event received, but there are active cell entries. This should not happen.");
                    // break; skipped on purpose
                }

                var playerId = pendingEvent.PlayerId;

                var playerQuery = _world.Query<PlayerScopeComponent, MetadataComponent>()
                    .HasValue<PlayerScopeComponent, PlayerId>(playerId);

                if (playerQuery.Count == 0)
                    return false; // exit loop

                var playerEntity = playerQuery.Entities.Single();
                var meta = playerEntity.GetComponent<MetadataComponent>();

                var playerEntry = new PlayerEntry
                {
                    PlayerId = playerId,
                    PlayerEntity = playerEntity,
                    PlayerNetworkId = meta.NetId,
                    CurrentAreaId = null,
                };

                _localPlayerEntry = playerEntry;
                _allPlayers.Add(playerId);
                _playerEntries.Add(playerId, playerEntry);

                _currentCellEntries.Clear();

                OnConnected?.Invoke(playerId, playerEntity);
                break;
            }
            case PendingEventKind.Disconnected:
            {
                var playerId = pendingEvent.PlayerId;

                if (_currentAreaEntry != null)
                {
                    var areaId = _currentAreaEntry.Value.AreaId;

                    // Destroy all active cell scope entities before clearing them
                    foreach (var cellEntry in _currentCellEntries)
                    {
                        // FIXME: This test should not be necessary. If this case triggers, we should do something about it, likely a sign of a deeper issue
                        if (cellEntry.CellEntity != default)
                        {
                            _netEntity.DeleteEntitiesInScope(cellEntry.CellEntity, true, true);
                        }
                        else
                        {
                            _logger.LogError("Cell entity was null. This shouldn't happen");
                        }

                        var wasMaster = IsLocalCellMaster(cellEntry.CellEntity);
                        OnDeactivatedCell?.Invoke(new FullCellId(areaId, cellEntry.CellId), cellEntry.CellEntity, wasMaster);
                    }
                    _currentCellEntries.Clear();

                    for (var i = 0; i < _currentAreaEntry.Value.AreaPlayers.Count;)
                    {
                        var otherPlayerId = _currentAreaEntry.Value.AreaPlayers[i];
                        if (otherPlayerId == playerId)
                        {
                            i++;
                            continue;
                        }

                        OnOtherPlayerOutsideArea?.Invoke(otherPlayerId, areaId, OtherPlayerOutsideAreaReason.NotifyBeforeSelfDisconnected);
                        _currentAreaEntry.Value.AreaPlayers.RemoveAt(i);
                        var otherPlayerEntry = _playerEntries[otherPlayerId];
                        otherPlayerEntry.CurrentAreaId = null;
                        _playerEntries[otherPlayerId] = otherPlayerEntry;
                    }

                    OnLeftArea?.Invoke(areaId, _currentAreaEntry.Value.AreaEntity);

                    _currentAreaEntry.Value.AreaPlayers.Clear();
                    var playerEntry = _playerEntries[playerId];
                    playerEntry.CurrentAreaId = null;
                    _playerEntries[playerId] = playerEntry;

                    // NOTE: The entity for the area scope gets deleted, however the ECS deletion message is NOT sent to the
                    // server. Scope entities are managed from the server and clients independently.
                    _netEntity.DeleteEntitiesInScope(_currentAreaEntry.Value.AreaEntity, true, true);
                    _currentAreaEntry = null;
                }

                for (var i = 0; i < _allPlayers.Count;)
                {
                    var otherPlayerId = _allPlayers[i];
                    if (otherPlayerId == playerId)
                    {
                        i++;
                        continue;
                    }

                    var otherPlayerEntry = _playerEntries[otherPlayerId];
                    OnOtherPlayerDeleted?.Invoke(otherPlayerId, otherPlayerEntry.PlayerEntity, OtherPlayerDeletedReason.NotifyBeforeSelfDisconnected);
                    _allPlayers.RemoveAt(i);
                    _playerEntries.Remove(otherPlayerId);
                }

                OnDisconnected?.Invoke(pendingEvent.PlayerId, _localPlayerEntry?.PlayerEntity, pendingEvent.disconnectedReason);

                _allPlayers.Clear();
                _playerEntries.Clear();
                _netEntity.DeleteAllNetworkedEntities(true);
                _localPlayerEntry = null;
                break;
            }
            case PendingEventKind.JoinedArea:
            {
                if (_localPlayerEntry == null)
                {
                    _logger.LogError("JoinedArea event received, but no local player entry found. This should not happen.");
                    break;
                }

                if (_currentAreaEntry != null)
                {
                    _logger.LogError("JoinedArea event received, but already in an area. This should not happen.");
                    _currentAreaEntry = null;
                    // break; skipped on purpose
                }

                var playerId = pendingEvent.PlayerId;
                var areaId = pendingEvent.AreaId;

                _logger.LogInformation("ECS JOINING (before query) {AreaId} by player {PlayerId}", areaId, playerId);

                var areaQuery = _world.Query<AreaScopeComponent, MetadataComponent>()
                    .HasValue<AreaScopeComponent, AreaId>(areaId);

                if (areaQuery.Count == 0)
                    return false;

                var areaEntity = areaQuery.Entities.First();

                if (areaEntity.GetComponent<AreaScopeComponent>().MasterClient == PlayerId.Invalid)
                    return false;

                var meta = areaEntity.GetComponent<MetadataComponent>();

                var areaEntry = new AreaEntry
                {
                    AreaId = areaId,
                    AreaEntity = areaEntity,
                    AreaNetworkId = meta.NetId,
                    AreaPlayers =
                    [
                        playerId
                    ],
                };

                _currentAreaEntry = areaEntry;

                var playerEntry = _playerEntries[playerId];
                playerEntry.CurrentAreaId = areaId;
                _playerEntries[playerId] = playerEntry;
                _localPlayerEntry = playerEntry;
                _currentCellEntries.Clear();

                _logger.LogInformation("ECS JOINING {AreaId} by player {PlayerId}", areaId, playerId);

                OnJoinedArea?.Invoke(areaId, areaEntity);
                break;
            }
            case PendingEventKind.LeftArea:
            {
                if (_localPlayerEntry == null)
                {
                    _logger.LogError("LeftArea event received, but no local player entry found. This should not happen.");
                    break;
                }

                if (_currentAreaEntry == null)
                {
                    _logger.LogWarning("LeftArea event received, but no current area entry found. This happens when a player leaves before the JoinedArea event is processed.");
                    break;
                }

                var playerId = pendingEvent.PlayerId;
                var areaId = _currentAreaEntry.Value.AreaId;
                _logger.LogInformation("ECS LEAVING {AreaId} by player {PlayerId}", areaId, playerId);

                // Destroy all active cell scope entities before clearing them
                foreach (var cellEntry in _currentCellEntries)
                {
                    // FIXME: This test should not be necessary. If this case triggers, we should do something about it, likely a sign of a deeper issue
                    if (cellEntry.CellEntity != default)
                    {
                        _netEntity.DeleteEntitiesInScope(cellEntry.CellEntity, true, true);
                    }
                    else
                    {
                        _logger.LogError("Cell entity was null. This shouldn't happen");
                    }

                    var wasMaster = IsLocalCellMaster(cellEntry.CellEntity);
                    OnDeactivatedCell?.Invoke(new FullCellId(areaId, cellEntry.CellId), cellEntry.CellEntity, wasMaster);
                }
                _currentCellEntries.Clear();

                for (var i = 0; i < _currentAreaEntry.Value.AreaPlayers.Count;)
                {
                    var otherPlayerId = _currentAreaEntry.Value.AreaPlayers[i];
                    if (otherPlayerId == playerId)
                    {
                        i++;
                        continue;
                    }

                    OnOtherPlayerOutsideArea?.Invoke(otherPlayerId, areaId, OtherPlayerOutsideAreaReason.NotifyBeforeSelfLeft);
                    _currentAreaEntry.Value.AreaPlayers.RemoveAt(i);
                    var otherPlayerEntry = _playerEntries[otherPlayerId];
                    otherPlayerEntry.CurrentAreaId = null;
                    _playerEntries[otherPlayerId] = otherPlayerEntry;
                }

                OnLeftArea?.Invoke(areaId, _currentAreaEntry.Value.AreaEntity);

                // NOTE: The entity for the area scope gets deleted, however the ECS deletion message is NOT sent to the
                // server. Scope entities are managed from the server and clients independently.
                _netEntity.DeleteEntitiesInScope(_currentAreaEntry.Value.AreaEntity, true, true);
                _currentAreaEntry = null;
                var localPlayer = _playerEntries[playerId];
                localPlayer.CurrentAreaId = null;
                _playerEntries[playerId] = localPlayer;
                _localPlayerEntry = localPlayer;
                break;
            }
            case PendingEventKind.OtherPlayerCreated:
            {
                if (_localPlayerEntry == null)
                {
                    _logger.LogError("OtherPlayerCreated event received, but no local player entry found. This should not happen.");
                    break;
                }

                var playerId = pendingEvent.PlayerId;

                if (_playerEntries.ContainsKey(playerId))
                {
                    _logger.LogError("OtherPlayerCreated event received for player {PlayerId}, but player entry already exists. This should not happen.", playerId);
                    _playerEntries.Remove(playerId);
                    // break; skipped on purpose
                }

                var playerQuery = _world.Query<PlayerScopeComponent, MetadataComponent>()
                    .HasValue<PlayerScopeComponent, PlayerId>(playerId);

                _logger.LogInformation("ECS OTHER CONNECTED player {PlayerId} (before query)", playerId);

                if (playerQuery.Count == 0)
                    return false; // exit loop

                _logger.LogInformation("ECS OTHER CONNECTED player {PlayerId}", playerId);

                var playerEntity = playerQuery.Entities.First();

                var meta = playerEntity.GetComponent<MetadataComponent>();

                var playerEntry = new PlayerEntry()
                {
                    PlayerId = playerId,
                    PlayerEntity = playerEntity,
                    PlayerNetworkId = meta.NetId,
                    CurrentAreaId = null,
                };

                _allPlayers.Add(playerId);
                _playerEntries.Add(playerId, playerEntry);

                var reason = pendingEvent.IsNotify
                    ? OtherPlayerCreatedReason.NotifyAfterSelfConnected
                    : OtherPlayerCreatedReason.OtherConnected;
                OnOtherPlayerCreated?.Invoke(playerId, playerEntity, reason);
                break;
            }
            case PendingEventKind.OtherPlayerDeleted:
            {
                if (_localPlayerEntry == null)
                {
                    _logger.LogError("OtherPlayerDeleted event received, but no local player entry found. This should not happen.");
                    break;
                }

                var playerId = pendingEvent.PlayerId;

                if (!_playerEntries.TryGetValue(playerId, out var playerEntry))
                {
                    _logger.LogWarning("OtherPlayerDeleted event received for player {PlayerId}, but no player entry found. This happens when the other player disconnects before the OtherPlayerCreated event is processed.", playerId);
                    break;
                }

                if (playerEntry.CurrentAreaId != null && playerEntry.CurrentAreaId == CurrentAreaId)
                {
                    var areaId = playerEntry.CurrentAreaId.Value;
                    OnOtherPlayerOutsideArea?.Invoke(playerId, areaId, OtherPlayerOutsideAreaReason.OtherDisconnected);
                    _currentAreaEntry!.Value.AreaPlayers.Remove(playerId);
                }

                OnOtherPlayerDeleted?.Invoke(playerId, playerEntry.PlayerEntity, OtherPlayerDeletedReason.OtherDisconnected);

                // NOTE: The entity for the player scope gets deleted, however the ECS deletion message is NOT sent to the
                // server. Scope entities are managed from the server and clients independently.
                _netEntity.DeleteEntitiesInScope(playerEntry.PlayerEntity, true, true);
                _allPlayers.Remove(playerId);
                _playerEntries.Remove(playerId);

                if (playerEntry.CurrentAreaId != null && playerEntry.CurrentAreaId == CurrentAreaId)
                {
                    _currentAreaEntry!.Value.AreaPlayers.Remove(playerId);
                }

                break;
            }
            case PendingEventKind.SetActiveCells:
            {
                if (_localPlayerEntry == null)
                {
                    _logger.LogError("SetActiveCells event received, but no local player entry found. This should not happen.");
                    break;
                }

                if (_currentAreaEntry == null)
                {
                    _logger.LogError("SetActiveCells event received, but local player is not in any area. This should not happen.");
                    break;
                }

                var newCellIds = pendingEvent.CellIds ?? [];
                var newCellEntries = new List<CellEntry>(newCellIds.Length);

                foreach (var cellId in newCellIds)
                {
                    // Cells are area-scoped: the same cell id can exist in different areas. The indexed
                    // FullCellId pairs the cell with its parent area, so this query only matches the cell
                    // scope entity that belongs to the player's current area.
                    var fullCellId = new FullCellId(_currentAreaEntry.Value.AreaId, cellId);

                    var cellQuery = _world.Query<CellScopeComponent, MetadataComponent>()
                        .HasValue<CellScopeComponent, FullCellId>(fullCellId);

                    // If cell entity doesn't exist yet, we need to wait for it to arrive in a network snapshot
                    if (cellQuery.Count == 0)
                        return false;

                    var cellEntity = cellQuery.Entities.First();

                    // If the cell's master client isn't set yet, we need to wait for it to arrive in a network snapshot
                    if (cellEntity.GetComponent<CellScopeComponent>().MasterClient == PlayerId.Invalid)
                        return false;

                    var meta = cellEntity.GetComponent<MetadataComponent>();

                    newCellEntries.Add(new CellEntry
                    {
                        CellId = cellId,
                        CellEntity = cellEntity,
                        CellNetworkId = meta.NetId,
                        CellPlayers = [],
                    });
                }

                // Currently active cells for diff.
                var previousCellIds = _currentCellEntries.Select(e => e.CellId).ToHashSet();
                var areaId = _currentAreaEntry.Value.AreaId;

                foreach (var existing in _currentCellEntries)
                {
                    if (!newCellIds.Contains(existing.CellId))
                    {
                        var wasMaster = IsLocalCellMaster(existing.CellEntity);
                        // FIXME: This test should not be necessary. If this case triggers, we should do something about it, likely a sign of a deeper issue
                        if (existing.CellEntity != default)
                        {
                            _netEntity.DeleteEntitiesInScope(existing.CellEntity, true, true);
                        }
                        else
                        {
			                _logger.LogError("Cell entity was null. This shouldn't happen");
                        }

                        OnDeactivatedCell?.Invoke(new FullCellId(areaId, existing.CellId), existing.CellEntity, wasMaster);
                    }
                }

                _currentCellEntries.Clear();
                _currentCellEntries.AddRange(newCellEntries);

                // Notify activation for cells that were not active before.
                foreach (var entry in newCellEntries)
                {
                    if (!previousCellIds.Contains(entry.CellId))
                    {
                        OnActivatedCell?.Invoke(new FullCellId(areaId, entry.CellId), entry.CellEntity, IsLocalCellMaster(entry.CellEntity));
                    }
                }

                var cellsString = _currentCellEntries.Aggregate("", (p, n) => p + (p == "" ? "" : ", ") + n.CellId.ToString());
                _logger.LogInformation("ECS set {count} active cells for player {PlayerId}: {cells}", _currentCellEntries.Count, pendingEvent.PlayerId, cellsString);
                break;
            }
            case PendingEventKind.OtherPlayerInsideArea:
            {
                if (_localPlayerEntry == null)
                {
                    _logger.LogError("OtherPlayerInsideArea event received, but no local player entry found. This should not happen.");
                    break;
                }

                if (_currentAreaEntry == null)
                {
                    _logger.LogError("OtherPlayerInsideArea event received, but no current area entry found. This should not happen.");
                    break;
                }

                var playerId = pendingEvent.PlayerId;

                if (!_playerEntries.TryGetValue(playerId, out var playerEntry))
                {
                    _logger.LogWarning("OtherPlayerInsideArea event received for player {PlayerId}, but no player entry found. This should not happen.", playerId);
                    break;
                }

                if (playerEntry.CurrentAreaId != null)
                {
                    _logger.LogError("OtherPlayerInsideArea event received for player {PlayerId}, but player is already inside area {CurrentAreaId}. This should not happen.",
                        playerId, playerEntry.CurrentAreaId);
                    playerEntry.CurrentAreaId = null;
                    _playerEntries[playerId] = playerEntry;
                    // NOTE: no break; here on purpose
                }

                var areaId = pendingEvent.AreaId;

                if (_currentAreaEntry.Value.AreaId != areaId)
                {
                    _logger.LogWarning("Received OtherPlayerInsideArea event for player {PlayerId} in area {AreaId}, but current area is {CurrentAreaId}",
                        playerId, areaId, _currentAreaEntry.Value.AreaId);
                    break;
                }

                var playerQuery = _world.Query<PlayerScopeComponent, MetadataComponent>()
                    .HasValue<PlayerScopeComponent, PlayerId>(playerId);

                _logger.LogInformation("ECS OTHER JOINING player {PlayerId} into area {AreaId} (before query)", playerId, areaId);

                if (playerQuery.Count == 0)
                    return false; // exit loop

                var areaQuery = _world.Query<AreaScopeComponent, MetadataComponent>()
                    .HasValue<AreaScopeComponent, AreaId>(areaId);

                if (areaQuery.Count == 0)
                    return false; // exit loop

                _logger.LogInformation("ECS OTHER JOINING player {PlayerId} into area {AreaId}", playerId, areaId);

                _currentAreaEntry.Value.AreaPlayers.Add(playerId);

                playerEntry.CurrentAreaId = areaId;
                _playerEntries[playerId] = playerEntry;

                var reason = pendingEvent.IsNotify
                    ? OtherPlayerInsideAreaReason.NotifyAfterSelfJoined
                    : OtherPlayerInsideAreaReason.OtherJoined;
                OnOtherPlayerInsideArea?.Invoke(playerId, areaId, reason);
                break;
            }
            case PendingEventKind.OtherPlayerOutsideArea:
            {
                if (_localPlayerEntry == null)
                {
                    _logger.LogError("OtherPlayerOutsideArea event received, but no local player entry found. This should not happen.");
                    break;
                }

                if (_currentAreaEntry == null)
                {
                    _logger.LogError("OtherPlayerOutsideArea event received, but no current area entry found. This should not happen.");
                    break;
                }

                var playerId = pendingEvent.PlayerId;

                if (!_playerEntries.TryGetValue(playerId, out var playerEntry))
                {
                    _logger.LogWarning("OtherPlayerOutsideArea event received for player {PlayerId}, but no player entry found. This happens when a player disconnects before the OtherPlayerOutsideArea event gets processed.", playerId);
                    break;
                }

                if (playerEntry.CurrentAreaId == null)
                {
                    _logger.LogWarning("OtherPlayerOutsideArea event received for player {PlayerId}, but player is already outside area. This happens when a player leaves before the OtherPlayerInsideArea event is processed.", playerId);
                    break;
                }

                var areaId = pendingEvent.AreaId;

                _currentAreaEntry.Value.AreaPlayers.Remove(playerId);

                playerEntry.CurrentAreaId = null;
                _playerEntries[playerId] = playerEntry;

                OnOtherPlayerOutsideArea?.Invoke(playerId, areaId, OtherPlayerOutsideAreaReason.OtherLeft);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(pendingEvent.Kind), pendingEvent.Kind, null);
        }

        return true;
    }
}
