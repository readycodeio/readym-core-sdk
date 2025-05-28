using System;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcEventAttribute(RelayMode relayMode) : Attribute;