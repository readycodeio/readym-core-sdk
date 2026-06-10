using System;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.RPC;

namespace ReadyM.Api.Multiplayer.Generators;

#pragma warning disable CS9113 // Parameter is unread.

/// <summary>
/// Marks a method as an RPC event handler.
/// Use this in partial classes extending from <see cref="ClientRpcHandler"/>.
/// </summary>
/// <param name="relayMode">Determines how the event is relayed to other clients.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcEventAttribute(RelayMode relayMode) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.