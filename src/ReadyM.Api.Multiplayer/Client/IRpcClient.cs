using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Client;

/// <exclude />
/// <summary>
/// Used internally in generated RPC handlers to handle sending and receiving RPC messages.
/// The type is public because it is used in mod-generated code, but it is not intended for direct use by mod developers.
/// </summary>
public interface IRpcClient : IPlayerIdProvider
{
    void AddClientRpcMessageHandler(RelayMessageCode eventCode, Action<CustomRelayEventHeader, NetDataReader> handler);
    void AddClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<CustomRelayEventHeader, NetDataReader> handler);
    void RemoveClientRpcMessageHandler(RelayMessageCode eventCode, Action<CustomRelayEventHeader, NetDataReader> handler);
    void RemoveClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<CustomRelayEventHeader, NetDataReader> handler);
    void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler);
    void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler);
    void SendMessage(RelayMessage message);
}