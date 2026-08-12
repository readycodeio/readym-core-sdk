using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Interop;

internal unsafe delegate void ServerRpcHandlerDelegate(ServerEventHeader header, byte* data, int size);
internal delegate void AddServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
internal delegate void RemoveServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
internal unsafe delegate void SendToOneDelegate(PlayerId player, byte* data, int size, DeliveryMethod delivery);
