using System;
using LiteNetLib;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Interop;

public delegate void ServerRpcHandlerDelegate(ServerEventHeader header, ReadOnlySpan<byte> data);
public delegate void AddServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
public delegate void RemoveServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
public delegate void SendToOneDelegate(PlayerId player, ReadOnlySpan<byte> data, DeliveryMethod delivery);
