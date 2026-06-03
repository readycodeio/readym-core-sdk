using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Interop;

public unsafe delegate void ServerRpcHandlerDelegate(ServerEventHeader header, byte* data, int size);
public delegate void AddServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
public delegate void RemoveServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
public unsafe delegate void SendToOneDelegate(PlayerId player, byte* data, int size, DeliveryMethod delivery);
