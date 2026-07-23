using System;

namespace ReadyM.Api.Multiplayer;

/// <summary>
/// Marks a <c>[ServerRpcContracts]</c> partial method as carrying the client-to-server
/// (request) shape of its RPC. Given this direction, the source generator emits a
/// <c>Send{Name}(...)</c> sender on the client and an <c>On{Name}(RpcContext, ...)</c>
/// handler stub on the server, both using this method's parameters.
///
/// A two-way RPC whose two directions have DIFFERENT shapes is declared as two overloads
/// sharing a name, one marked <see cref="ClientToServerAttribute"/> and the other
/// <see cref="ServerToClientAttribute"/>. A two-way RPC whose directions share an
/// IDENTICAL shape (they would be duplicate overloads) is instead a single method carrying
/// BOTH attributes. Every contract method must carry at least one direction attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ClientToServerAttribute : Attribute;

/// <summary>
/// Marks a <c>[ServerRpcContracts]</c> partial method as carrying the server-to-client
/// (response or push) shape of its RPC. Given this direction, the source generator emits a
/// <c>Send{Name}(PlayerId recipient, ...)</c> sender on the server and an <c>On{Name}(...)</c>
/// handler stub on the client, both using this method's parameters.
///
/// See <see cref="ClientToServerAttribute"/> for how the two directions combine into a
/// one-way, symmetric two-way, or asymmetric two-way RPC.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ServerToClientAttribute : Attribute;
