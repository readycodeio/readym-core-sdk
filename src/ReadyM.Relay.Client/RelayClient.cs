using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client
{
    public sealed class RelayClient : RelayPeerBase, IDisposable
    {
        private readonly Guid _userGuid;
        private readonly string _host;
        private readonly int _port;

        private readonly Random _rng = new();
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _client;
        private readonly Action<LogLevel, string, object?[]> _logger;

        private Thread? _clientThread;
        private bool _isRunning;

        public Room RoomState { get; }
        public Player LocalPlayer { get; set; } = new(new Dictionary<object, object>());
        public ConcurrentDictionary<int, Player> OtherPlayers { get; } = new();

        public IEnumerable<Player> AllPlayers => OtherPlayers.Values.Append(LocalPlayer);

        public bool InRoom { get; private set; }

        public event Action<Dictionary<object, object?>>? OnRoomPropertiesChanged;
        public event Action<int, Dictionary<object, object?>>? OnPlayerPropertiesChanged;
        public event Action<CustomEventHeader, NetPacketReader>? OnCustomEvent;
        public event Action? OnJoinedRoom;
        public event Action<DisconnectReason>? OnDisconnected;
        public event Action<int>? OnPingUpdated;

        /// <summary>
        /// At this point the connecting player has been assigned an ID and we have synced their state.
        /// </summary>
        public event Action<int>? OnOtherPlayerJoined;

        public event Action<int>? OnOtherPlayerLeft;


        private NetPeer? Server
        {
            get
            {
                if (_client.FirstPeer == null)
                {
                    Log(LogLevel.Error, "Disconnected from server");
                }

                return _client.FirstPeer;
            }
        }

        public RelayClient(Guid userGuid, string host, int port, Action<LogLevel, string, object?[]> logger)
        {
            _userGuid = userGuid;
            _host = host;
            _port = port;

            _listener = new EventBasedNetListener();
            _listener.NetworkReceiveEvent += OnListenerOnNetworkReceiveEvent;
            _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;
            _listener.PeerDisconnectedEvent += OnServerDisconnected;

            _client = new NetManager(_listener)
            {
                AutoRecycle = true,
                EnableStatistics = true,
                DisconnectOnUnreachable = true
            };
            _logger = logger;

            RoomState = new Room(this);
        }

        private void OnServerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            InRoom = false;
            OnDisconnected?.Invoke(disconnectinfo.Reason);
        }

        public void Start()
        {
            _client.Start();
            _client.Connect(_host, _port, _userGuid.ToString());

            _isRunning = true;
            _clientThread = new Thread(() =>
            {
                Log(LogLevel.Information, "Running relay client on port {0}", _port);
                while (_isRunning)
                {
                    _client.PollEvents();
                    Thread.Sleep(15);
                }
            });

            _clientThread.Start();
        }

        public void Stop()
        {
            _client.Stop(true);
            _isRunning = false;
            _clientThread?.Join();
            _clientThread = null;
            OnDisconnected?.Invoke(DisconnectReason.DisconnectPeerCalled);
        }

        public Player? GetPlayerState(int playerId)
        {
            return playerId == LocalPlayer.PeerId ? LocalPlayer : OtherPlayers.GetValueOrDefault(playerId);
        }

        public void OpSetCustomPropertiesOfActor(int playerId, Dictionary<object, object?> data)
        {
            if (!InRoom)
            {
                if (playerId == Constants.UnsetPlayerId)
                {
                    UpdateAndGetDiff(LocalPlayer.Properties, data);
                }
                else
                {
                    Log(LogLevel.Warning, "Attempted to set properties of player {0} while not in room", playerId);
                }

                return;
            }

            var writer = CreatePlayerPropertiesUpdatePacket(playerId, data);
            Server?.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void OpSetCustomPropertiesOfRoom(Dictionary<object, object?> data)
        {
            var writer = CreateRoomPropertiesUpdatePacket(data);
            Server?.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Send an event to a specific player or group of players.
        /// This overload does not support event caching, as cached events must either be sent to all other players or all players.
        /// </summary>
        public void OpRaiseEvent(byte eventCode, object? data, int[] peers, DeliveryMethod deliveryMethod)
        {
            var writer = new NetDataWriter();
            writer.PutCustomEventHeader(eventCode, LocalPlayer.PeerId, peers, EventCaching.DoNotCache);

            if (data != null)
            {
                SerializeObject(writer, data);
            }

            Log(LogLevel.Debug, "Sending event {0}", eventCode);
            Server?.Send(writer, deliveryMethod);
        }

        /// <summary>
        /// Send an event with a specific delivery method. This overload does not support event caching.
        /// </summary>
        public void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod)
        {
            var writer = new NetDataWriter();
            writer.PutCustomEventHeader(eventCode, LocalPlayer.PeerId, mode, EventCaching.DoNotCache);

            if (data != null)
            {
                SerializeObject(writer, data);
            }

            Log(LogLevel.Debug, "Sending event {0}", eventCode);
            Server?.Send(writer, deliveryMethod);
        }

        /// <summary>
        /// Send an event the will be cached by the server and sent to all/other players (depending on the eventCaching parameter).
        /// </summary>
        public void OpRaiseEvent(byte eventCode, object? data, EventCaching eventCaching)
        {
            var writer = new NetDataWriter();

            // AddToRoomCacheGlobal events are sent to all players, AddToRoomCache - to others, DoNotCache - too, by default
            var mode = eventCaching == EventCaching.AddToRoomCacheGlobal ? RelayMode.All : RelayMode.Others;
            writer.PutCustomEventHeader(eventCode, LocalPlayer.PeerId, mode, eventCaching);

            if (data != null)
            {
                SerializeObject(writer, data);
            }

            Log(LogLevel.Debug, "Sending event {0}", eventCode);
            Server?.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void Dispose()
        {
            Stop();
        }

        private void Log(LogLevel level, [StructuredMessageTemplate] string message, params object?[] values)
        {
            _logger(level, $"[Relay Client] {message}", values);
        }

        private void OnListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliverymethod)
        {
            var eventCode = reader.GetByte();

            switch ((SystemEvent)eventCode)
            {
                case SystemEvent.PeerIdAssigned:
                {
                    LocalPlayer.PeerId = reader.GetInt();
                    Log(LogLevel.Information, "Assigned Actor ID {0}", LocalPlayer.PeerId);

                    // send joined room event
                    var writer = new NetDataWriter();
                    writer.Put((byte)SystemEvent.JoinRoomRequest);
                    SerializeObject(writer, LocalPlayer.Properties);
                    Server?.Send(writer, DeliveryMethod.ReliableOrdered);

                    return;
                }
                case SystemEvent.PlayerStateChanged:
                {
                    var playerId = reader.GetInt();
                    var changes = DeserializeObject<Dictionary<object, object?>>(reader);

                    if (playerId == LocalPlayer.PeerId)
                    {
                        var diff = UpdateAndGetDiff(LocalPlayer.Properties, changes);
                        OnPlayerPropertiesChanged?.Invoke(playerId, diff);
                    }
                    else
                    {
                        if (!OtherPlayers.TryGetValue(playerId, out var player))
                        {
                            Log(LogLevel.Debug, "Received initial state for player {0}", playerId);
                            OtherPlayers[playerId] = new Player(changes
                                .Where(x => x.Value != null)
                                .ToDictionary(x => x.Key, x => x.Value!));
                        }
                        else
                        {
                            var diff = UpdateAndGetDiff(player.Properties, changes);
                            OnPlayerPropertiesChanged?.Invoke(playerId, diff);
                        }
                    }

                    return;
                }
                case SystemEvent.RoomStateChanged:
                {
                    var changes = DeserializeObject<Dictionary<object, object?>>(reader);
                    var diff = UpdateAndGetDiff(RoomState.Properties, changes);
                    OnRoomPropertiesChanged?.Invoke(diff);
                    return;
                }
                case SystemEvent.PlayerJoined:
                {
                    var playerId = reader.GetInt();
                    var initialState = DeserializeObject<Dictionary<object, object>>(reader);
                    var newPlayer = new Player(initialState);

                    if (playerId == LocalPlayer.PeerId)
                    {
                        LocalPlayer = newPlayer;
                        InRoom = true;
                        OnJoinedRoom?.Invoke();
                    }
                    else
                    {
                        if (!OtherPlayers.TryAdd(playerId, newPlayer))
                        {
                            Log(LogLevel.Warning, "Player {0} already exists", playerId);
                            OtherPlayers[playerId] = newPlayer;
                        }

                        OnOtherPlayerJoined?.Invoke(playerId);
                    }

                    return;
                }
                case SystemEvent.PlayerLeft:
                {
                    var playerId = reader.GetInt();
                    OnOtherPlayerLeft?.Invoke(playerId);
                    return;
                }
                case SystemEvent.JoinRoomRequest:
                    Log(LogLevel.Error, "Join room request received, why?!");
                    return;
            }

            Log(LogLevel.Debug, "Received custom event {0}", eventCode);
            var header = reader.GetCustomEventHeader(eventCode);
            OnCustomEvent?.Invoke(header, reader);
        }

        private void OnNetworkLatencyUpdateEvent(NetPeer peer, int latency)
        {
            // Round trip time. LiteNetLib reports one way latency, so we double it.
            // We add a random jitter so that the results are not always divisible by 2.
            OnPingUpdated?.Invoke(2 * latency + _rng.Next(2));
        }

        private NetDataWriter CreatePlayerPropertiesUpdatePacket(int playerId, Dictionary<object, object?> changes)
        {
            var writer = new NetDataWriter();
            writer.Put((byte)SystemEvent.PlayerStateChanged);
            writer.Put(playerId);
            SerializeObject(writer, changes);
            return writer;
        }

        private NetDataWriter CreateRoomPropertiesUpdatePacket(Dictionary<object, object?> changes)
        {
            var writer = new NetDataWriter();
            writer.Put((byte)SystemEvent.RoomStateChanged);
            SerializeObject(writer, changes);
            return writer;
        }
    }
}