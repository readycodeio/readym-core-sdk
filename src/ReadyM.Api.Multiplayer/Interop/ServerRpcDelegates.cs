using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Interop;

public unsafe delegate void ServerRpcHandlerDelegate(ServerEventHeader header, byte* data, int dataLength);
public delegate void AddServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
public delegate void RemoveServerRpcMessageHandlerDelegate(RelayMessageCode eventCode, ServerRpcHandlerDelegate handler);
