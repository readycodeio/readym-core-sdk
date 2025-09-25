using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Engine.ECS;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.ECS.Archetypes;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.ECS.Jobs;

namespace ReadyM.Relay.Client.State;

public class ClientState : IDisposable
{
    private enum PendingEventKind
    {
        Connected,
        Disconnected,
        JoinedArea,
        LeftArea,
        OtherPlayerCreated,
        OtherPlayerDeleted,
        OtherPlayerInsideArea,
        OtherPlayerOutsideArea
    }

    private struct PendingEvent
    {
        public bool Invalidated;
        public PendingEventKind Kind;
        public AreaId AreaId;
        public PlayerId PlayerId;
        public bool IsNotify;
        public DisconnectReason DisconnectReason;
    }

    public struct AreaEntry
    {
        public AreaId AreaId { get; internal set; }
        public Entity AreaEntity { get; internal set; }
        public NetworkId AreaNetworkId { get; internal set; }

        public List<PlayerId> AreaPlayers { get; internal set; }
    }

    public struct PlayerEntry
    {
        public PlayerId PlayerId { get; internal set; }
        public Entity PlayerEntity { get; internal set; }
        public NetworkId PlayerNetworkId { get; internal set; }
        public AreaId? CurrentAreaId { get; internal set; }
    }

    private readonly Store _world;
    private readonly NetworkedEntityManager _netEntity;
    private readonly IRelayClient _relayClient;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly JobRegistry _jobRegistry;
    private readonly ILogger _logger;

    private readonly List<PlayerId> _allPlayers = [];
    private readonly Dictionary<PlayerId, PlayerEntry> _playerEntries = new();

    // NOTE: For performance. The same as _pendingPlayerEntries[_localPlayerId]
    private PlayerEntry? _localPlayerEntry;

    private AreaEntry? _currentAreaEntry;

    private readonly object _lock = new();
    private readonly List<PendingEvent> _pendingEvents = [];

    public bool IsConnected
        => _localPlayerEntry != null;

    public PlayerId? LocalPlayerId
        => _localPlayerEntry?.PlayerId;

    public PlayerEntry? LocalPlayerEntry
        => _localPlayerEntry;

    public Entity? LocalPlayerEntity
        => _localPlayerEntry?.PlayerEntity;

    public Api.Helpers.ReadOnlyList<PlayerId> AllPlayers => new(_allPlayers);
    public Api.Helpers.ReadOnlyList<PlayerId> OtherPlayers => new([.. _allPlayers.Where(p => p != LocalPlayerId)]);
    public System.Collections.ObjectModel.ReadOnlyDictionary<PlayerId, PlayerEntry> PlayerEntries => new(_playerEntries);
    public Api.Helpers.ReadOnlyList<PlayerId> AreaPlayers => (_currentAreaEntry?.AreaPlayers).NullableWrapReadOnly();
    public Api.Helpers.ReadOnlyList<PlayerId> OtherAreaPlayers => (_currentAreaEntry?.AreaPlayers.Where(p => p != LocalPlayerId).ToList()).NullableWrapReadOnly();

    public bool JoinedArea
        => _currentAreaEntry is { } value && value.AreaId != AreaId.Invalid;

    public AreaId? CurrentAreaId
        => _currentAreaEntry?.AreaId;

    public AreaEntry? CurrentAreaEntry
        => _currentAreaEntry;

    public Entity? CurrentAreaEntity
        => _currentAreaEntry?.AreaEntity;

    public event Action<PlayerId, Entity>? OnConnected;
    public event Action<PlayerId, Entity?, DisconnectReason>? OnDisconnected;
    public event Action<PlayerId, Entity, OtherPlayerCreatedReason>? OnOtherPlayerCreated;
    public event Action<PlayerId, Entity, OtherPlayerDeletedReason>? OnOtherPlayerDeleted;

    public event Action<AreaId, Entity>? OnJoinedArea;
    public event Action<AreaId, Entity>? OnLeftArea;
    public event Action<PlayerId, AreaId, OtherPlayerInsideAreaReason>? OnOtherPlayerInsideArea;
    public event Action<PlayerId, AreaId, OtherPlayerOutsideAreaReason>? OnOtherPlayerOutsideArea;

    public ArchetypeId AreaArchetype { get; }
    public ArchetypeId PlayerArchetype { get; }

    public ClientState(
        Store world,
        NetworkedEntityManager netEntity,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        JobRegistry jobRegistry,
        DefaultAreaArchetypeRegistration areaArchetype,
        DefaultPlayerArchetypeRegistration playerArchetype,
        ILogger logger)
    {
        _world = world;
        _netEntity = netEntity;
        _relayClient = relayClient;
        _ecsLoop = ecsLoop;
        _jobRegistry = jobRegistry;
        _logger = logger;

        AreaArchetype = areaArchetype.AreaArchetype;
        PlayerArchetype = playerArchetype.PlayerArchetype;

        _ecsLoop.OnUpdateLoop += ProcessPendingEvents;

        _relayClient.OnConnected += OnConnectedHandler;
        _relayClient.OnDisconnected += OnDisconnectedHandler;
        _relayClient.OnJoinedArea += OnJoinedAreaHandler;
        _relayClient.OnLeftArea += OnLeftAreaHandler;
        _relayClient.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        _relayClient.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        _relayClient.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;

        _jobRegistry.OnApplySnapshot += OnApplySnapshotHandler;
    }

    public void Dispose()
    {
        _ecsLoop.OnUpdateLoop -= ProcessPendingEvents;

        _jobRegistry.OnApplySnapshot -= OnApplySnapshotHandler;

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

        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
    }

    private void OnDisconnectedHandler(IRelayClientNetworkThreadContext context, DisconnectReason disconnectReason)
    {
        lock (_lock)
        {
            _pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.Disconnected,
                PlayerId = context.PlayerId ?? PlayerId.Invalid,
                DisconnectReason = disconnectReason,
            });
        }

        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
    }

    private void OnJoinedAreaHandler(IRelayClientNetworkThreadContext context, AreaId areaId)
    {
        lock (_lock)
        {
            _pendingEvents.Add(new PendingEvent()
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

        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
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

        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
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

        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
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

        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
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

        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
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

        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
    }

    private void OnApplySnapshotHandler()
    {
        _ecsLoop.Scheduler.Schedule(static (_, self) => { self.ProcessPendingEvents(); }, this);
    }

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
                    or PendingEventKind.LeftArea
                    or PendingEventKind.OtherPlayerInsideArea
                    or PendingEventKind.OtherPlayerOutsideArea => true,
                _ => throw new ArgumentOutOfRangeException()
            };

        int? lastConnectedIndex = null;
        var playerId = _localPlayerEntry?.PlayerId;

        int? lastJoinedIndex = null;
        var areaId = _currentAreaEntry?.AreaId;

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

                    lastJoinedIndex = null;
                    areaId = null;

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

                    if (lastJoinedIndex != null)
                    {
                        // NOTE: Assume the previous areaId should be assumed left. Remove all events
                        // from the previous areaId, but keep the current event.
                        InvalidateRange(lastJoinedIndex.Value, i - 1, IsAreaEvent);
                    }

                    lastJoinedIndex = i;
                    areaId = pendingEvent.AreaId;
                    break;
                }
                case PendingEventKind.LeftArea:
                {
                    if (playerId == null)
                    {
                        pendingEvent.Invalidated = true;
                        break;
                    }

                    if (lastJoinedIndex != null)
                    {
                        // NOTE: Remove all events from the current areaId, including the current event.
                        InvalidateRange(lastJoinedIndex.Value, i, IsAreaEvent);
                        pendingEvent.Invalidated = true;
                    }

                    lastJoinedIndex = null;
                    areaId = null;
                    areaPlayers.Clear();
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

                    if (areaPlayers.TryGetValue(pendingEvent.PlayerId, out var otherPlayerJoinedIndex))
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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _pendingEvents[i] = pendingEvent;
        }
    }

    private void ProcessPendingEvents(CommandBufferSynced _) => ProcessPendingEvents();

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

                var playerId = pendingEvent.PlayerId;


                var playerQuery = _world.Query<PlayerScopeComponent, MetadataComponent>()
                    .HasValue<PlayerScopeComponent, PlayerId>(playerId);

                if (playerQuery.Count == 0)
                    return false; // exit loop

                var playerEntity = playerQuery.Entities.First();
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

                OnConnected?.Invoke(playerId, playerEntity);
                break;
            }
            case PendingEventKind.Disconnected:
            {
                var playerId = pendingEvent.PlayerId;

                if (_currentAreaEntry != null)
                {
                    var areaId = _currentAreaEntry.Value.AreaId;
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
                    _netEntity.DeleteEntitiesInScope(_currentAreaEntry.Value.AreaEntity, true);
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

                OnDisconnected?.Invoke(pendingEvent.PlayerId, _localPlayerEntry?.PlayerEntity, pendingEvent.DisconnectReason);

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

                _logger.LogInformation("ECS LEAVING {AreaId} by player {PlayerId}", areaId, playerId);

                _netEntity.DeleteEntitiesInScope(_currentAreaEntry.Value.AreaEntity, true);
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
                _netEntity.DeleteEntitiesInScope(playerEntry.PlayerEntity, true);
                _allPlayers.Remove(playerId);
                _playerEntries.Remove(playerId);

                if (playerEntry.CurrentAreaId != null && playerEntry.CurrentAreaId == CurrentAreaId)
                {
                    _currentAreaEntry!.Value.AreaPlayers.Remove(playerId);
                }

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