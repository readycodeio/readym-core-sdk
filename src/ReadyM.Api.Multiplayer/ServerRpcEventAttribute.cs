using System;

namespace ReadyM.Api.Multiplayer;

#pragma warning disable CS9113 // Parameter is unread.

/// <exclude />
[AttributeUsage(AttributeTargets.Method)]
public sealed class ServerRpcEventAttribute(string name) : Attribute;

/// <exclude />
[AttributeUsage(AttributeTargets.Method)]
[Obsolete]
public sealed class LegacyServerRpcHandlerAttribute(string name) : Attribute;

/// <exclude />
[AttributeUsage(AttributeTargets.Method)]
public sealed class ServerRpcHandlerAttribute(string name) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.