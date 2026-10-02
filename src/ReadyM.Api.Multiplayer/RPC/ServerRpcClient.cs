using System;

namespace ReadyM.Api.Multiplayer.RPC;

/// <summary>
/// Base class for RPC handlers.
/// </summary>
[Obsolete("Part of old 0.x SDK. Just use [ServerRpcFor] without any base class.")]
public abstract class ServerRpcClient : RpcBase;