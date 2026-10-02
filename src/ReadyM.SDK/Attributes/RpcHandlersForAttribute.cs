namespace ReadyM.SDK.Attributes;

/// <summary>Binds a class to the [RpcContracts] set it implements.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RpcHandlersForAttribute(Type contractsType) : Attribute
{
    public Type ContractsType { get; } = contractsType;
}