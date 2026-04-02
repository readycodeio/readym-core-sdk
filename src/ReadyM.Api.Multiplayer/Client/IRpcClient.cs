using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Client;

public interface IRpcClient : IPlayerIdProvider
{
    void AddClientRpcMessageHandler(RelayMessageCode eventCode, Action<CustomRelayEventHeader, NetDataReader> handler);
    void AddClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<CustomRelayEventHeader, NetDataReader> handler);
    void RemoveClientRpcMessageHandler(RelayMessageCode eventCode, Action<CustomRelayEventHeader, NetDataReader> handler);
    void RemoveClientRpcMessageHandler(RelayMessageCode minEventCode, RelayMessageCode maxEventCode, Action<CustomRelayEventHeader, NetDataReader> handler);
    void SendMessage(RelayMessage message);
}