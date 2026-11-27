using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Native.Container;

namespace ReadyM.Api.Multiplayer.Interop;

/// <summary>Runs a mod's handler for a message; the dimension is where the sender is.</summary>
internal unsafe delegate void ServerRpcHandlerDelegate(ServerEventHeader header, DimensionId dimension, byte* data, int size);
internal delegate void AddServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
internal delegate void RemoveServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
internal unsafe delegate void SendToOneDelegate(PlayerId player, byte* data, int size, DeliveryMethod delivery);

internal delegate void NameMessageCodeDelegate(RelayMessageCode code, NativeString256 name);
