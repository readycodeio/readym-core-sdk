using System;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer;

/// <summary>
/// Generates a Send(...) on the client and an On(...) handler on the server.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ClientToServerAttribute : Attribute;

/// <summary>
/// Generates a Send(...) on the server and an On(...) handler on the client.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ServerToClientAttribute : Attribute;

/// <summary>
/// Generates a Send(...) and an On(...) handler on the client.
/// </summary>
/// <param name="relayMode">Determines which other clients receive the message.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ClientToClientsAttribute(RelayMode relayMode) : Attribute;