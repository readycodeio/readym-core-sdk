using System;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Generators;

#pragma warning disable CS9113 // Parameter is unread.

/// <summary>
/// Marks a method as an RPC event handler.
/// Use this in partial classes extending from <see cref="ReadyM.Api.Multiplayer.RPC.RpcClassBase"/>.
/// </summary>
/// <param name="relayMode">Determines how the event is relayed to other clients.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcEventAttribute(RelayMode relayMode) : Attribute;
#pragma warning re store CS9113 // Parameter is unread.