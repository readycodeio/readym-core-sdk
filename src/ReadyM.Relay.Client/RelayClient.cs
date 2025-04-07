using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
        private const string Host = "68.154.30.5";
        private const int Port = 7134;
        
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _client;
        private readonly Action<LogLevel, string, object?[]> _logger;

        private Thread? _clientThread;
        private bool _isRunning;

        public Room RoomState { get; } = new();
        public Player LocalPlayer { get; set; } = new(new Dictionary<object, object>());
        public ConcurrentDictionary<int, Player> OtherPlayers { get; } = new();

        public bool InRoom { get; private set; }

        public event Action<Dictionary<object, object?>>? OnRoomPropertiesChanged;
        public event Action<int, Dictionary<object, object?>>? OnPlayerPropertiesChanged;
        public event Action<CustomEventHeader, NetPacketReader>? OnCustomEvent;
        public event Action? OnJoinedRoom;
        public event Action<DisconnectReason>? OnDisconnected;


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

        public RelayClient(Action<LogLevel, string, object?[]> logger)
        {
            _listener = new EventBasedNetListener();
            _listener.NetworkReceiveEvent += OnListenerOnNetworkReceiveEvent;
            _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdateEvent;
            _listener.PeerDisconnectedEvent += OnServerDisconnected;

            _client = new NetManager(_listener)
            {
                AutoRecycle = true,
            };
            _logger = logger;
        }

        private void OnServerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            InRoom = false;
            OnDisconnected?.Invoke(disconnectinfo.Reason);
        }

        public void Start()
        {
            _client.Start();
            _client.Connect(Host, Port, "Wukong"); // TODO: JWT

            _isRunning = true;
            _clientThread = new Thread(() =>
            {
                Log(LogLevel.Information, "Running relay client on port {0}", Port);
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
            _isRunning = false;
            _client.Stop();
            _clientThread?.Join();
            _clientThread = null;
        }

        public Player? GetPlayerState(int playerId)
        {
            return OtherPlayers.GetValueOrDefault(playerId);
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

        public void OpRaiseEvent(byte eventCode, object? data, RelayMode mode, DeliveryMethod deliveryMethod)
        {
            var writer = new NetDataWriter();
            writer.PutCustomEventHeader(eventCode, LocalPlayer.ActorNumber, mode);

            if (data != null)
            {
                SerializeObject(writer, data);
            }

            Log(LogLevel.Debug, "Sending event {0}", eventCode);
            Server?.Send(writer, deliveryMethod);
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
                case SystemEvent.ActorNumberAssigned:
                {
                    LocalPlayer.ActorNumber = reader.GetInt();
                    Log(LogLevel.Information, "Assigned Actor ID {0}", LocalPlayer.ActorNumber);

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

                    Dictionary<object, object?> diff;
                    if (playerId == LocalPlayer.ActorNumber)
                    {
                        diff = UpdateAndGetDiff(LocalPlayer.Properties, changes);
                    }
                    else
                    {
                        if (!OtherPlayers.TryGetValue(playerId, out var player))
                        {
                            Log(LogLevel.Warning, "Player {0} not found", playerId);
                            return;
                        }

                        diff = UpdateAndGetDiff(player.Properties, changes);
                    }

                    OnPlayerPropertiesChanged?.Invoke(playerId, diff);
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

                    if (playerId == LocalPlayer.ActorNumber)
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
            Log(LogLevel.Debug, "Network latency updated: {0}ms", latency);
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