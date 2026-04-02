using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Client;

/// <summary>
/// `IRelayClient` is a transport layer. All it is responsible for is wrapping buffers of data in appropriate
/// packets / headers so that they can be addressed to the right players. It also handles player ID assignment.
/// It does NOT handle any protocol specific logic. In particular, it should know nothing of:
/// 1) specific RPC calls
/// 2) ECS components and their state.
/// This is important because:
/// 1) There will be many implementations of `IRelayClient` and we want to keep these implementations minimal and
/// with as little duplication as possible.
/// 2) Relay client cannot be responsible for the entire protocol anyway. Therefore, by the principle of single responsibility
/// it cannot be responsible for just part of the protocol. It would create a confusion as to which parts of the
/// protocol belong where exactly. Over time this would lead to a difficult to maintain code.
/// `IRelayClient` has to know about areas of interest, because these are necessary to handle routing efficiently.
/// In particular, for most use-cases we don't want send messages using a recipient list but rather using the area
/// of interest.
/// `IRelayClient` also has to know of entity ownership, at least to the extent that this feature is used for
/// addressing messages. In particular, one of the addressing modes allows us to indicate that the entity should be
/// relayed to the owner of the entity. This owner cannot be pre-concretized because while the message is in flight,
/// ownership could be transferred to another player. Retaining the owner addressing mode allows the old owner to
/// re-relay the message to the new owner in that specific rare case.
/// </summary>
internal interface IRelayClient : IRpcClient, IDisposable
{
    bool RequestedConnect { get; }

    AreaId? RequestedAreaId { get; }

    /// <summary>
    /// Fired immediately after the client requests connection to the server. The client is not yet connected when
    /// this is fired.
    /// Always called from the thread calling `Start()`.
    /// </summary>
    event Action OnStart;

    /// <summary>
    /// Fired immediately after the client requests disconnection from the server. This will NOT fire if the client
    /// got disconnected from the server unwillingly, e.g. due to a network error.
    /// Always called from the MAIN thread.
    /// </summary>
    event Action OnRequestedStop;

    event Action OnRequestedConnect;

    /// <summary>
    /// Fired when the client-server handshake got completed and the client is able to receive
    /// and send messages to/from the server. Before the `OnConnected` event `IRelayClient` guarantees that it will
    /// not fire any events related to messages from the server. Sending messages to the server before the `OnConnected`
    /// event fires will result in an exception.
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext, PlayerId, uint>? OnConnected;

    event Action OnRequestedDisconnect;

    /// <summary>
    /// Fired when an expected or unexpected disconnection from the server. After this event is fired,
    /// no further events related to messages from the server will be fired untile successful reconnection. Similarly,
    /// attempting to send messages in the disconnected state will result in an exception.
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext, DisconnectReason>? OnDisconnected;

    /// <summary>
    /// Fired when another player has connected to the server. This will fire for all players regardless
    /// of their area of interest. This is so that we can enumerate all players regardless of where we are in the game,
    /// e.g. show statistics about the total number of players on the server. Or to be able to send whisper messages
    /// to players in different areas of interest.
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext, PlayerId> OnOtherPlayerConnected;

    /// <summary>
    /// Fired when another player has been disconnected from the server. This is the counterpart to
    /// `OnOtherPlayerConnected`.
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext, PlayerId> OnOtherPlayerDisconnected;

    /// <summary>
    /// Fired immediately after the client requests to join an area of interest. The client is not yet in the area
    /// of interest when this is fired.
    /// Always called from the MAIN thread.
    /// </summary>
    event Action<AreaId>? OnRequestedJoinArea;

    /// <summary>
    /// Fired when the client has successfully joined an area of interest. Before this event is fired, the client
    /// will not receive any messages addressed to the area of interest. The client will always leave an area of
    /// interest before joining a new one. It is currently impossible to be in multiple areas of interest at the same
    /// time.
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext, AreaId> OnJoinedArea;

    /// <summary>
    /// Fired immediately after the client requests to leave an area of interest. The client has not yet left the
    /// area of interest when this is fired. In particular, it is possible to still receive messages addressed to the
    /// "stale" area of interest before the `OnLeftArea` event is fired. 
    /// Always called from the MAIN thread.
    /// </summary>
    event Action? OnRequestedLeaveArea;

    /// <summary>
    /// Fired when the client has successfully left an area of interest. Before joining another area of interest,
    /// the client will not receive any area of interest addressed messages. The client is considered to be in no
    /// area of interest at this point (similarly to when it first gets connected to the server).
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext> OnLeftArea;

    /// <summary>
    /// Fired when another player joins our area of interest. We are not going to be informed about players joining
    /// areas of interest where we ourselves are not joined. When we join a new area of interest, this event will fire
    /// for each player already in that area of interest. If a new player joins the area of interest, this will fire
    /// as well. We will always first be informed of a new player connecting to the server before we get informed about
    /// it joining our area of interest. Even when dealing with a reconnection.
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerJoinedArea;

    /// <summary>
    /// Counterpart to `OnOtherPlayerJoinedArea`. Fires when another player leaves our area of interest. This will
    /// also fire for players that were in the area of interest and got disconnected. This will fire before the
    /// `OnOtherPlayerDisconnected` event in that case. When leaving an area of interest, we will receive this event
    /// for each player that was in the area of interest before we left and that we knew of. Also, will be fired if
    /// a player gets disconnected or if they leave the area of interest on request.
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext, PlayerId>? OnOtherPlayerLeftArea;

    /// <summary>
    /// Used to measure ping. Currently only measures round-trip ping to the server.
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<int>? OnPingUpdated;

    /// <summary>
    /// Fired for each message successfully received by us. The following messages will be received:
    /// 1) Directly addressed to us.
    /// 2) Address to all players in the area of interest where we are.
    /// 3) Global messages send to everyone on the server regardless of area of interest. TODO: Rate limiting?
    /// Always called from the same NETWORK thread.
    /// </summary>
    event Action<ServerEventHeader, NetDataReader>? OnAnyBuiltInMessage;

    event Action<ServerEventHeader, NetDataReader>? OnAnyServerRpcMessage;
    event Action<CustomRelayEventHeader, NetDataReader>? OnAnyClientRpcMessage;

    void AddBuiltInMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler);
    void AddBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler);
    void RemoveBuiltInMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler);
    void RemoveBuiltInMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler);

    void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler);
    void AddServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler);
    void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler);
    void RemoveServerRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<ServerEventHeader, NetDataReader> handler);

    /// <summary>
    /// Fired on each server update tick.
    /// This is called from the thread calling `RunAsync()`.
    /// </summary>
    event Action<IRelayClientNetworkThreadContext>? OnClientUpdate;

    PendingActionScheduler<IRelayClientNetworkThreadContext> Scheduler { get; }

    int GetMaxPacketSize(DeliveryMethod deliveryMethod);

    // NOTE: These are worker-thread side methods.
    void Start();
    Task RunAsync(CancellationToken token);
    void Stop();

    // NOTE: These are user side methods.
    // TODO: Separate into two different interfaces
    void RequestConnect();
    void RequestDisconnect();
    void RequestReconnect();

    void RequestJoinArea(AreaId areaId);
    void RequestLeaveArea();

    void SendRawMessage(NetDataWriter writer, DeliveryMethod deliveryMethod);

    void SendMessageToServer<T>(RelayMessageCode eventCode, T data, DeliveryMethod deliveryMethod)
        where T : INetSerializable;

    void SendMessageToPeers<T>(RelayMessageCode eventCode, T data, PlayerId[] peers, DeliveryMethod deliveryMethod)
        where T : INetSerializable;

    void SendMessageRelayMode<T>(RelayMessageCode eventCode, T data, RelayMode mode, DeliveryMethod deliveryMethod)
        where T : INetSerializable;

    void LogEventStats();
}