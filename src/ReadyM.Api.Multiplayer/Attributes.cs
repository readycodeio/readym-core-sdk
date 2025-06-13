using System;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

[AttributeUsage(AttributeTargets.Method)]
#pragma warning disable CS9113 // Parameter is unread.
public sealed class RpcEventAttribute(RelayMode relayMode, EventCaching caching = EventCaching.DoNotCache) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.

[AttributeUsage(AttributeTargets.Struct)]
public sealed class DeriveINetSerializableAttribute : Attribute;