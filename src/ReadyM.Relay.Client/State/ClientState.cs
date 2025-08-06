using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Friflo.Engine.ECS;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.ECS.Registry;

namespace ReadyM.Relay.Client.State;

public class ClientState : IDisposable
{
    private class RegisterAreaComponentsCallback(EntityBuilder builder) : IAreaComponentRegistryCallback
    {
        public void AcceptComponent<T>(IAreaComponentRegistry registry)
            where T : struct, IComponent
        {
            builder.Add<T>();
        }
    }
    
    private class RegisterPlayerComponentsCallback(EntityBuilder builder) : IPlayerComponentRegistryCallback
    {
        public void AcceptComponent<T>(IPlayerComponentRegistry registry)
            where T : struct, IComponent
        {
            builder.Add<T>();
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
        OtherPlayerOutsideArea
    }
    
    private struct PendingEvent
    {
        public PendingEventKind Kind;
        public AreaId AreaId;
        public PlayerId PlayerId;
        public bool IsNotify;
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
    private readonly IRelayClient _relayClient;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly ClientNetworkedStateSynchronizer _synchronizer;
    private readonly ILogger _logger;

    private readonly ArchetypeId _areaArchetype;
    private readonly ArchetypeId _playerArchetype;

    public ArchetypeId AreaArchetype => _areaArchetype;
    public ArchetypeId PlayerArchetype => _playerArchetype;
    
    private readonly List<PlayerId> _allPlayers = new List<PlayerId>();
    private readonly Dictionary<PlayerId, PlayerEntry> _playerEntries = new Dictionary<PlayerId, PlayerEntry>();

    // NOTE: For performance. The same as _pendingPlayerEntries[_localPlayerId]
    private PlayerEntry? _localPlayerEntry;

    private AreaEntry? _currentAreaEntry = new()
    {
        AreaPlayers = new List<PlayerId>(),
    };
    
    private readonly List<PendingEvent> _pendingEvents = new List<PendingEvent>();

    public bool IsConnected
        => _localPlayerEntry != null;

    public PlayerId? LocalPlayerId
        => _localPlayerEntry?.PlayerId;

    public PlayerEntry? LocalPlayerEntry
        => _localPlayerEntry;

    public Entity? LocalPlayerEntity
        => _localPlayerEntry?.PlayerEntity;

    public ReadyM.Api.Helpers.ReadOnlyList<PlayerId> AllPlayers => new(_allPlayers);
    public ReadOnlyDictionary<PlayerId, PlayerEntry> PlayerEntries => new(_playerEntries);

    public bool JoinedArea
        => _currentAreaEntry is { } value && value.AreaId != AreaId.Invalid;

    public AreaId? CurrentAreaId
        => _currentAreaEntry?.AreaId;

    public AreaEntry? CurrentAreaEntry
        => _currentAreaEntry;

    public Entity? CurrentAreaEntity
        => _currentAreaEntry?.AreaEntity;
    
    public event Action<PlayerId, Entity>? OnConnected;
    public event Action<PlayerId, Entity>? OnDisconnected;
    public event Action<PlayerId, Entity, OtherPlayerCreatedReason>? OnOtherPlayerCreated;
    public event Action<PlayerId, Entity, OtherPlayerDeletedReason>? OnOtherPlayerDeleted;

    public event Action<AreaId, Entity>? OnJoinedArea;
    public event Action<AreaId, Entity>? OnLeftArea;
    public event Action<PlayerId, AreaId, OtherPlayerInsideAreaReason>? OnOtherPlayerInsideArea;
    public event Action<PlayerId, AreaId, OtherPlayerOutsideAreaReason>? OnOtherPlayerOutsideArea;
    
    public ClientState(
        Store world,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        ClientNetworkedStateSynchronizer synchronizer,
        IAreaComponentRegistry areaComponentRegistry,
        IPlayerComponentRegistry playerComponentRegistry,
        ILogger logger)
    {
        _world = world;
        _relayClient = relayClient;
        _ecsLoop = ecsLoop;
        _synchronizer = synchronizer;
        _logger = logger;
        
        _areaArchetype = world.RegisterArchetype(b =>
        {
            b.Add<MetadataComponent>();
            b.Add<AreaScopeComponent>();
            areaComponentRegistry.Accept(new RegisterAreaComponentsCallback(b));
        });
        _playerArchetype = world.RegisterArchetype(b =>
        {
            b.Add<MetadataComponent>();
            b.Add<PlayerScopeComponent>();
            playerComponentRegistry.Accept(new RegisterPlayerComponentsCallback(b));
        });
        
        _relayClient.OnConnected += OnConnectedHandler;
        _relayClient.OnDisconnected += OnDisconnectedHandler;
        _relayClient.OnJoinedArea += OnJoinedAreaHandler;
        _relayClient.OnLeftArea += OnLeftAreaHandler;
        _relayClient.OnOtherPlayerConnected += OnOtherPlayerConnectedHandler;
        _relayClient.OnOtherPlayerDisconnected += OnOtherPlayerDisconnectedHandler;
        _relayClient.OnOtherPlayerJoinedArea += OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnOtherPlayerLeftArea += OnOtherPlayerLeftAreaHandler;
        
        _synchronizer.OnEcsSnapshot += OnEcsSnapshotHandler;
    }

    public void Dispose()
    {
        _synchronizer.OnEcsSnapshot -= OnEcsSnapshotHandler;
        
        _relayClient.OnOtherPlayerLeftArea -= OnOtherPlayerLeftAreaHandler;
        _relayClient.OnOtherPlayerJoinedArea -= OnOtherPlayerJoinedAreaHandler;
        _relayClient.OnOtherPlayerDisconnected -= OnOtherPlayerDisconnectedHandler;
        _relayClient.OnOtherPlayerConnected -= OnOtherPlayerConnectedHandler;
        _relayClient.OnLeftArea -= OnLeftAreaHandler;
        _relayClient.OnJoinedArea -= OnJoinedAreaHandler;
        _relayClient.OnDisconnected -= OnDisconnectedHandler;
        _relayClient.OnConnected -= OnConnectedHandler;
    }

    private void OnConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        _ecsLoop.Scheduler.Schedule((_, self, context0, playerId0) =>
        {
            // NOTE: Connection event is always appended, even if there's a disconnected event already in the queue
            self._pendingEvents.Add(new PendingEvent()
            {
               Kind = PendingEventKind.Connected,
               PlayerId = playerId0,
            });
            foreach (var otherPlayerId in context0.AllPlayers)
            {
                if (otherPlayerId == playerId0)
                    continue;
                self._pendingEvents.Add(new PendingEvent()
                {
                    Kind = PendingEventKind.OtherPlayerCreated,
                    PlayerId = otherPlayerId,
                    IsNotify = true,
                });
            }
        }, this, context, playerId);
    }

    private void OnDisconnectedHandler(IRelayClientNetworkThreadContext context, DisconnectReason disconnectReason)
    {
        _ecsLoop.Scheduler.Schedule((_, self, context0) =>
        {
            // NOTE: Disconnection event invalidates all other events in the queue
            self._pendingEvents.Clear();
            self._pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.Disconnected,
                PlayerId = context0.PlayerId,
            });
        }, this, context);
    }

    private void OnJoinedAreaHandler(IRelayClientNetworkThreadContext context, AreaId areaId)
    {
        _ecsLoop.Scheduler.Schedule((_, self, context0, areaId0) =>
        {
            // NOTE: Join area event is always appended, even if there's a left area event already in the queue
            self._pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.JoinedArea,
                AreaId = areaId0,
                PlayerId = context0.PlayerId,
            });
            foreach (var otherPlayerId in context0.AreaPlayers)
            {
                if (otherPlayerId == context0.PlayerId)
                    continue;
                self._pendingEvents.Add(new PendingEvent()
                {
                    Kind = PendingEventKind.OtherPlayerInsideArea,
                    PlayerId = otherPlayerId,
                    IsNotify = true,
                });
            }
        }, this, context, areaId);
    }

    private void OnLeftAreaHandler(IRelayClientNetworkThreadContext context)
    {
        _ecsLoop.Scheduler.Schedule((_, self, context0) =>
        {
            if (self._currentAreaEntry == null)
            {
                self._logger.LogWarning("LeftArea event received, but no current area. This should not happen.");
                return;
            }
            
            var areaId = self._currentAreaEntry.Value.AreaId;
            for (var i = 0; i < self._pendingEvents.Count;)
            {
                var pendingEvent = self._pendingEvents[i];
                if (pendingEvent.Kind == PendingEventKind.JoinedArea)
                {
                    // Cancel any pending JoinedArea event because we are leaving the area
                    areaId = pendingEvent.AreaId;
                    self._pendingEvents.RemoveAt(i);
                }
                else if (pendingEvent.Kind == PendingEventKind.LeftArea && pendingEvent.AreaId == areaId)
                {
                    // NOTE: There should be at most one LeftArea and one JoinedArea event in the queue at the same time
                    throw new InvalidOperationException();
                }
                else if (pendingEvent.Kind == PendingEventKind.OtherPlayerInsideArea && pendingEvent.AreaId == areaId)
                {
                    // Cancel any pending OtherPlayerJoinedArea events for the same area that we are leaving
                    self._pendingEvents.RemoveAt(i);
                }
                else if (pendingEvent.Kind == PendingEventKind.OtherPlayerOutsideArea && pendingEvent.AreaId == areaId)
                {
                    // Cancel any pending OtherPlayerLeftArea events for the same area that we are leaving
                    self._pendingEvents.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            self._pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.LeftArea,
                AreaId = self._currentAreaEntry.Value.AreaId,
                PlayerId = context0.PlayerId,
            });
        }, this, context);
    }

    private void OnOtherPlayerConnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        _ecsLoop.Scheduler.Schedule((_, self, playerId0) =>
        {
            // NOTE: Other player connection event is always appended, even if there's a disconnection event already in the queue
            self._pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.OtherPlayerCreated,
                PlayerId = playerId0,
            });
        }, this, playerId);
    }

    private void OnOtherPlayerDisconnectedHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        _ecsLoop.Scheduler.Schedule((_, self, playerId0) =>
        {
            for (var i = 0; i < self._pendingEvents.Count;)
            {
                var pendingEvent = self._pendingEvents[i];
                if (pendingEvent.Kind == PendingEventKind.OtherPlayerCreated && pendingEvent.PlayerId == playerId0)
                {
                    // Cancel any pending OtherPlayerConnected event for the same player that is disconnecting
                    self._pendingEvents.RemoveAt(i);
                }
                else if (pendingEvent.Kind == PendingEventKind.OtherPlayerDeleted && pendingEvent.PlayerId == playerId0)
                {
                    // NOTE: There should be at most one OtherPlayerDisconnected event in the queue at the same time
                    throw new InvalidOperationException();
                }
                else if (pendingEvent.Kind == PendingEventKind.OtherPlayerInsideArea && pendingEvent.PlayerId == playerId0)
                {
                    // Cancel any pending JoinedArea event for the same player that is disconnecting
                    self._pendingEvents.RemoveAt(i);
                }
                else if (pendingEvent.Kind == PendingEventKind.OtherPlayerOutsideArea && pendingEvent.PlayerId == playerId0)
                {
                    // Cancel any pending LeftArea event for the same player that is disconnecting
                    self._pendingEvents.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            self._pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.OtherPlayerDeleted,
                PlayerId = playerId0,
            });
        }, this, playerId);
    }

    private void OnOtherPlayerJoinedAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        _ecsLoop.Scheduler.Schedule((_, self, context0, playerId0) =>
        {
            // NOTE: Other player join area event is always appended, even if there's a left area event already in the queue
            self._pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.OtherPlayerInsideArea,
                AreaId = context0.CurrentArea,
                PlayerId = playerId0,
            });
        }, this, context, playerId);
    }

    private void OnOtherPlayerLeftAreaHandler(IRelayClientNetworkThreadContext context, PlayerId playerId)
    {
        _ecsLoop.Scheduler.Schedule((_, self, context0, playerId0) =>
        {
            if (self._currentAreaEntry == null)
            {
                self._logger.LogWarning("OtherPlayerLeftArea event received, but no current area. This should not happen.");
                return;
            }
            
            var areaId = self._currentAreaEntry.Value.AreaId;
            for (var i = 0; i < self._pendingEvents.Count;)
            {
                var pendingEvent = self._pendingEvents[i];
                if (pendingEvent.Kind == PendingEventKind.JoinedArea)
                {
                    // Adjust areaId to the last joined area
                    areaId = pendingEvent.AreaId;
                    i++;
                }
                else if (pendingEvent.Kind == PendingEventKind.LeftArea)
                {
                    // Adjust areaId to the last left area
                    areaId = default;
                    i++;
                }
                else if (pendingEvent.Kind == PendingEventKind.OtherPlayerInsideArea && pendingEvent.PlayerId == playerId0 && pendingEvent.AreaId == areaId)
                {
                    // Cancel any pending OtherPlayerJoinedArea event for the same player that is leaving the area
                    self._pendingEvents.RemoveAt(i);
                }
                else if (pendingEvent.Kind == PendingEventKind.OtherPlayerOutsideArea && pendingEvent.PlayerId == playerId0 && pendingEvent.AreaId == areaId)
                {
                    // NOTE: There should be at most one OtherPlayerLeftArea event in the queue at the same time
                    throw new InvalidOperationException();
                }
                else
                {
                    i++;
                }
            }
            self._pendingEvents.Add(new PendingEvent()
            {
                Kind = PendingEventKind.OtherPlayerOutsideArea,
                AreaId = context0.CurrentArea,
                PlayerId = playerId0,
            });
        }, this, context, playerId);
    }
    
    private void OnEcsSnapshotHandler()
    {
        _ecsLoop.Scheduler.Schedule((_, self) =>
        {
            while (self._pendingEvents.Count > 0)
            {
                var pendingEvent = self._pendingEvents[0];

                switch (pendingEvent.Kind)
                {
                    case PendingEventKind.Connected:
                    {
                        var playerId = pendingEvent.PlayerId;
                        var playerQuery = self._world.Query<PlayerScopeComponent, MetadataComponent>()
                            .HasValue<PlayerScopeComponent, PlayerId>(playerId);

                        if (playerQuery.Count == 0)
                            goto finish;

                        if (self._localPlayerEntry == null)
                        {
                            self._logger.LogWarning("Connected event received, but no local player entry found. This should not happen.");
                            break;
                        }
                        
                        var meta = self._localPlayerEntry.Value.PlayerEntity.GetComponent<MetadataComponent>();
                        
                        var playerEntity = playerQuery.Entities.First();
                        var playerEntry = new PlayerEntry()
                        {
                            PlayerId = playerId,
                            PlayerEntity = playerEntity,
                            PlayerNetworkId = meta.NetId,
                            CurrentAreaId = null,
                        };

                        self._localPlayerEntry = playerEntry;
                        self._allPlayers.Add(playerId);
                        self._playerEntries.Add(playerId, playerEntry);
                        
                        self.OnConnected?.Invoke(playerId, playerEntity);
                        break;
                    }
                    case PendingEventKind.Disconnected:
                    {
                        if (self._localPlayerEntry == null)
                        {
                            self._logger.LogWarning("Disconnected event received, but no local player entry found. This should not happen.");
                            break;
                        }
                        
                        var playerId = pendingEvent.PlayerId;
                        
                        if (self._currentAreaEntry != null)
                        {
                            var areaId = self._currentAreaEntry.Value.AreaId;
                            while (self._currentAreaEntry.Value.AreaPlayers.Count > 0)
                            {
                                var otherPlayerId = self._currentAreaEntry.Value.AreaPlayers[0];
                                if (otherPlayerId != playerId)
                                    self.OnOtherPlayerOutsideArea?.Invoke(otherPlayerId, areaId, OtherPlayerOutsideAreaReason.NotifyBeforeSelfDisconnected);
                                self._currentAreaEntry.Value.AreaPlayers.RemoveAt(0);
                                var otherPlayerEntry = self._playerEntries[otherPlayerId];
                                otherPlayerEntry.CurrentAreaId = null;
                                self._playerEntries[otherPlayerId] = otherPlayerEntry;
                            }

                            self.OnLeftArea?.Invoke(areaId, self._currentAreaEntry.Value.AreaEntity);
                            self._currentAreaEntry = null;
                        }

                        while (self._allPlayers.Count > 0)
                        {
                            var otherPlayerId = self._allPlayers[0];
                            if (otherPlayerId != playerId)
                            {
                                var otherPlayerEntry = self._playerEntries[otherPlayerId];
                                self.OnOtherPlayerDeleted?.Invoke(otherPlayerId, otherPlayerEntry.PlayerEntity, OtherPlayerDeletedReason.NotifyBeforeSelfDisconnected);
                            }
                            self._allPlayers.RemoveAt(0);
                            self._playerEntries.Remove(otherPlayerId);
                        }
                        
                        self.OnDisconnected?.Invoke(pendingEvent.PlayerId, self._localPlayerEntry.Value.PlayerEntity);
                        self._localPlayerEntry = null;
                        break;
                    }
                    case PendingEventKind.JoinedArea:
                    {
                        var playerId = pendingEvent.PlayerId;
                        var areaId = pendingEvent.AreaId;
                        var areaQuery = self._world.Query<AreaScopeComponent, MetadataComponent>()
                            .HasValue<AreaScopeComponent, AreaId>(areaId);
                        
                        if (areaQuery.Count == 0)
                            goto finish;
                        
                        var areaEntity = areaQuery.Entities.First();

                        var meta = areaEntity.GetComponent<MetadataComponent>();

                        var areaEntry = new AreaEntry()
                        {
                            AreaId = areaId,
                            AreaEntity = areaEntity,
                            AreaNetworkId = meta.NetId,
                            AreaPlayers = new List<PlayerId>()
                            {
                                playerId,
                            },
                        };

                        self._currentAreaEntry = areaEntry;

                        var playerEntry = self._playerEntries[playerId];
                        playerEntry.CurrentAreaId = areaId;
                        self._playerEntries[playerId] = playerEntry;
                        self._localPlayerEntry = playerEntry;
                        
                        self.OnJoinedArea?.Invoke(areaId, areaEntity);
                        break;
                    }
                    case PendingEventKind.LeftArea:
                    {
                        if (self._currentAreaEntry == null)
                        {
                            self._logger.LogWarning("LeftArea event received, but no current area entry found. This should not happen.");
                            break;
                        }
                        
                        var playerId = pendingEvent.PlayerId;
                        var areaId = self._currentAreaEntry.Value.AreaId;
                        
                        while (self._currentAreaEntry.Value.AreaPlayers.Count > 0)
                        {
                            var otherPlayerId = self._currentAreaEntry.Value.AreaPlayers[0];
                            if (otherPlayerId == playerId)
                                continue;
                            self.OnOtherPlayerOutsideArea?.Invoke(otherPlayerId, areaId, OtherPlayerOutsideAreaReason.NotifyBeforeSelfLeft);
                            self._currentAreaEntry.Value.AreaPlayers.RemoveAt(0);
                            var otherPlayerEntry = self._playerEntries[otherPlayerId];
                            otherPlayerEntry.CurrentAreaId = null;
                            self._playerEntries[otherPlayerId] = otherPlayerEntry;
                        }

                        self.OnLeftArea?.Invoke(areaId, self._currentAreaEntry.Value.AreaEntity);
                        
                        self._currentAreaEntry = null;
                        var localPlayer = self._playerEntries[playerId];
                        localPlayer.CurrentAreaId = null;
                        self._playerEntries[playerId] = localPlayer;
                        self._localPlayerEntry = localPlayer;
                        break;
                    }
                    case PendingEventKind.OtherPlayerCreated:
                    {
                        var playerId = pendingEvent.PlayerId;
                        
                        var playerQuery = self._world.Query<PlayerScopeComponent, MetadataComponent>()
                            .HasValue<PlayerScopeComponent, PlayerId>(playerId);

                        if (playerQuery.Count == 0)
                            goto finish;

                        var playerEntity = playerQuery.Entities.First();

                        var meta = playerEntity.GetComponent<MetadataComponent>();
                        
                        var playerEntry = new PlayerEntry()
                        {
                            PlayerId = playerId,
                            PlayerEntity = playerEntity,
                            PlayerNetworkId = meta.NetId,
                            CurrentAreaId = null,
                        };
                        
                        self._allPlayers.Add(playerId);
                        self._playerEntries.Add(playerId, playerEntry);

                        var reason = pendingEvent.IsNotify
                            ? OtherPlayerCreatedReason.NotifyAfterSelfConnected
                            : OtherPlayerCreatedReason.OtherConnected;
                        self.OnOtherPlayerCreated?.Invoke(playerId, playerEntity, reason);
                        break;
                    }
                    case PendingEventKind.OtherPlayerDeleted:
                    {
                        var playerId = pendingEvent.PlayerId;
                        
                        var playerEntry = self._playerEntries[playerId];
                        if (playerEntry.CurrentAreaId != null && playerEntry.CurrentAreaId == CurrentAreaId)
                        {
                            var areaId = playerEntry.CurrentAreaId.Value;
                            self.OnOtherPlayerOutsideArea?.Invoke(playerId, areaId, OtherPlayerOutsideAreaReason.OtherDisconnected);
                            self._currentAreaEntry!.Value.AreaPlayers.Remove(playerId);
                        }
                        
                        self.OnOtherPlayerDeleted?.Invoke(playerId, playerEntry.PlayerEntity, OtherPlayerDeletedReason.OtherDisconnected);
                        self._allPlayers.Remove(playerId);
                        self._playerEntries.Remove(playerId);
                        
                        if (playerEntry.CurrentAreaId != null && playerEntry.CurrentAreaId == CurrentAreaId)
                        {
                            self._currentAreaEntry!.Value.AreaPlayers.Remove(playerId);
                        }
                        
                        break;
                    }
                    case PendingEventKind.OtherPlayerInsideArea:
                    {
                        if (self._currentAreaEntry == null)
                        {
                            self._logger.LogWarning("OtherPlayerInsideArea event received, but no current area entry found. This should not happen.");
                            break;
                        }
                        
                        var playerId = pendingEvent.PlayerId;
                        var areaId = pendingEvent.AreaId;
                        
                        var playerQuery = self._world.Query<PlayerScopeComponent, MetadataComponent>()
                            .HasValue<PlayerScopeComponent, PlayerId>(playerId);
                        
                        if (playerQuery.Count == 0)
                            goto finish;
                        
                        var areaQuery = self._world.Query<AreaScopeComponent, MetadataComponent>()
                            .HasValue<AreaScopeComponent, AreaId>(areaId);
                        
                        if (areaQuery.Count == 0)
                            goto finish;

                        if (self._currentAreaEntry.Value.AreaId == areaId)
                        {
                            self._currentAreaEntry.Value.AreaPlayers.Add(playerId);
                        }

                        var playerEntry = self._playerEntries[playerId];
                        playerEntry.CurrentAreaId = areaId;
                        self._playerEntries[playerId] = playerEntry;
                        
                        var reason = pendingEvent.IsNotify
                            ? OtherPlayerInsideAreaReason.NotifyAfterSelfJoined
                            : OtherPlayerInsideAreaReason.OtherJoined;
                        self.OnOtherPlayerInsideArea?.Invoke(playerId, areaId, reason);
                        break;
                    }
                    case PendingEventKind.OtherPlayerOutsideArea:
                    {
                        if (self._currentAreaEntry == null)
                        {
                            self._logger.LogWarning("OtherPlayerOutsideArea event received, but no current area entry found. This should not happen.");
                            break;
                        }
                        
                        var playerId = pendingEvent.PlayerId;
                        var areaId = pendingEvent.AreaId;

                        self._currentAreaEntry.Value.AreaPlayers.Remove(playerId);
                        
                        var playerEntry = self._playerEntries[playerId];
                        playerEntry.CurrentAreaId = null;
                        self._playerEntries[playerId] = playerEntry;

                        self.OnOtherPlayerOutsideArea?.Invoke(playerId, areaId, OtherPlayerOutsideAreaReason.OtherLeft);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(pendingEvent.Kind), pendingEvent.Kind, null);
                }
            }
            
            finish: return;
        }, this);
    }
}