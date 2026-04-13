using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal class DeriveMemberInfo(
    string name,
    ITypeSymbol type,
    int order,
    bool readOnly,
    bool isInvalid)
{
    public string Name { get; } = name;
    public ITypeSymbol Type { get; } = type;
    public int Order { get; } = order;
    public bool ReadOnly { get; } = readOnly;
    public bool IsInvalid { get; } = isInvalid;
}