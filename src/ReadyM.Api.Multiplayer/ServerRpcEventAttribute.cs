using System;

namespace ReadyM.Api.Multiplayer;

/// <exclude />
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServerRpcContractsAttribute : Attribute;

/// <summary>
/// Binds an RPC class (server <c>ServerRpcHandlersBase</c>, client <c>ServerRpcClient</c>) to the
/// <see cref="ServerRpcContractsAttribute"/> class it implements, and is required on both.
/// A project may reference several contract sets, directly or transitively; this is what tells the
/// generator which one to emit against. Only the legs declared by the named class are generated,
/// so one contract set per RPC class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServerRpcForAttribute(Type contractsType) : Attribute
{
    /// <summary>The <c>[ServerRpcContracts]</c> class this RPC class implements.</summary>
    public Type ContractsType { get; } = contractsType;
}
