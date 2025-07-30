using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Common.Protocol;

namespace ReadyM.Api.Multiplayer.Server;

public interface IRelayServer : IDisposable
{
    bool IsRunning { get; }

    PendingActionScheduler<IRelayServerNetworkThreadContext> Scheduler { get; }
    
    Task StartAsync(CancellationToken token);
    Task RunAsync(CancellationToken token);
    void Stop();

    /// <summary>
    /// The server startup will await all async tasks registered to this event.
    /// </summary>
    public event Func<CancellationToken, Task>? OnServerStarting;
    
    /// <summary>
    /// Fired after the server has started.
    /// This is called from the thread calling StartAsync().
    /// </summary>
    event Action? OnServerStarted;
    
    /// <summary>
    /// Fired when the server is stopped.
    /// This is called from the thread calling Stop().
    /// </summary>
    event Action? OnServerStopped;
    
    /// <summary>
    /// Fired on each server update tick.
    /// This is called from the thread calling `RunAsync()`.
    /// </summary>
    event Action<IRelayServerNetworkThreadContext>? OnServerUpdate;

    /// <summary>
    /// Fired when a player connects to the server.
    /// This is called from the thread calling `RunAsync()`.
    /// </summary>
    event Action<IRelayServerNetworkThreadContext, PlayerId, Guid>? OnPlayerConnected;
    event Action<IRelayServerNetworkThreadContext, PlayerId, Guid, DisconnectReason>? OnPlayerDisconnected;
    
    event Action<IRelayServerNetworkThreadContext, AreaId>? OnAreaCreated;
    event Action<IRelayServerNetworkThreadContext, AreaId>? OnAreaDeleted;
    event Action<IRelayServerNetworkThreadContext, PlayerId, AreaId>? OnPlayerJoinedArea;
    event Action<IRelayServerNetworkThreadContext, PlayerId, AreaId>? OnPlayerLeftArea;

    event Func<RelayConnectionOptions, ConnectionRequest, bool>? OnCanConnect;
    
    void AddBuiltInMessageHandler(RelayMessageCode eventCode, Action<IRelayServerNetworkThreadContext, ServerEventHeader, NetDataReader> handler);
    void RemoveBuiltInMessageHandler(RelayMessageCode eventCode, Action<IRelayServerNetworkThreadContext, ServerEventHeader, NetDataReader> handler);
    void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<IRelayServerNetworkThreadContext, ServerEventHeader, NetDataReader> handler);
    void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<IRelayServerNetworkThreadContext, ServerEventHeader, NetDataReader> handler);
 
    // NOTE: We cannot introduce methods like `SendToAllGlobal` or `SendToAllAreaOfInterest` here because that would
    // encourage race conditions. In between the moment we issue a call and the moment the call is executed, the list
    // of recipients could change leading to unexpected behavior (e.g. sending the same snapshot message twice to the
    // same player, omitting a freshly joining player when sending a message, etc.)
    void SendToOne(PlayerId playerId, NetDataWriter writer, DeliveryMethod deliveryMethod);
    void SendToAll(ReadOnlyList<PlayerId> playerIds, NetDataWriter writer, DeliveryMethod deliveryMethod);
    void SendToAllExcept(ReadOnlyList<PlayerId> playerIds, PlayerId exceptPlayerId, NetDataWriter writer, DeliveryMethod deliveryMethod);

    int GetMaxPacketSize();
}
