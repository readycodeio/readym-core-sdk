using System;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

[AttributeUsage(AttributeTargets.Method)]
#pragma warning disable CS9113 // Parameter is unread.
public sealed class ServerRpcEventAttribute(ServerRpcCode rpcCode) : Attribute;
public sealed class ServerRpcHandlerAttribute(ServerRpcCode rpcCode) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.
