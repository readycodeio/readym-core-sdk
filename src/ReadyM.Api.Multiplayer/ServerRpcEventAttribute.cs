using System;

namespace ReadyM.Api.Multiplayer;

#pragma warning disable CS9113 // Parameter is unread.

/// <exclude />
[AttributeUsage(AttributeTargets.Method)]
[Obsolete]
public sealed class LegacyServerRpcHandlerAttribute(string name) : Attribute;

/// <exclude />
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServerRpcContractsAttribute : Attribute;
#pragma warning restore CS9113 // Parameter is unread.