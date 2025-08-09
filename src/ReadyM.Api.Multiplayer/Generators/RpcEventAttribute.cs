using System;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Generators;

[AttributeUsage(AttributeTargets.Method)]
#pragma warning disable CS9113 // Parameter is unread.
public sealed class RpcEventAttribute(RelayMode relayMode) : Attribute;
#pragma warning re store CS9113 // Parameter is unread.