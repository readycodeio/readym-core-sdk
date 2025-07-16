using System;

namespace ReadyM.Api.Multiplayer;

[AttributeUsage(AttributeTargets.Method)]
#pragma warning disable CS9113 // Parameter is unread.
public sealed class ServerRpcEventAttribute(string name) : Attribute;
public sealed class ServerRpcHandlerAttribute(string name) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.
