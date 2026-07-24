using System;

namespace ReadyM.Api.Multiplayer;

/// <summary>
/// Client-to-server (request) leg of a contract method: generates a Send on the client and an
/// On(RpcContext, ...) handler on the server. Different-shaped two-way RPCs are two overloads
/// (one per direction); identical-shape two-way is one method with both attributes. Every contract
/// method needs at least one direction attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ClientToServerAttribute : Attribute;

/// <summary>
/// Server-to-client (response/push) leg of a contract method: generates a Send(PlayerId, ...) on
/// the server and an On(...) handler on the client. See <see cref="ClientToServerAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ServerToClientAttribute : Attribute;
