using System;

namespace ReadyM.Api.Multiplayer;

/// <exclude />
[Obsolete("Part of old 0.x SDK. Use [RpcContracts] instead.")]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServerRpcContractsAttribute : Attribute;

/// <summary>
/// Binds an RPC class to the <see cref="ServerRpcContractsAttribute"/> class it implements, and is
/// required on both sides. The class needs no base: the generated half supplies the right one for
/// the side it is compiled on. A project may reference several contract sets, directly or
/// transitively; this is what tells the generator which one to emit against. Only the legs declared
/// by the named class are generated, so one contract set per RPC class.
/// </summary>
[Obsolete("Part of old 0.x SDK. Use [RpcHandlersFor] instead.")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ServerRpcForAttribute(Type contractsType) : Attribute
{
    /// <summary>The <c>[ServerRpcContracts]</c> class this RPC class implements.</summary>
    public Type ContractsType { get; } = contractsType;
}
