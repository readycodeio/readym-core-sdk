using ReadyM.Api.Multiplayer;

namespace ReadyM.SDK.Attributes;

/// <summary>
/// Marks the class as containing RPC contracts.
/// Each must be a <c>public static partial void</c> method with serializable parameters.
/// Each must be annotated with one of the direction attributes:<br/>
/// * <see cref="ClientToServerAttribute"/><br/>
/// * <see cref="ServerToClientAttribute"/>
/// * <see cref="ClientToClientsAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RpcContractsAttribute : Attribute;
