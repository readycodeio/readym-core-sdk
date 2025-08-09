using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal class GeneratorTypeInfo(
    string name,
    string @namespace,
    (string Name, ITypeSymbol Type, int Order, bool ReadOnly)[] members,
    bool isNullable,
    string[] errorMessages)
{
    public string Name { get; } = name;
    public string Namespace { get; set; } = @namespace;
    public (string Name, ITypeSymbol Type, int Order, bool ReadOnly)[] Members { get; } = members;
    public bool IsNullable { get; set; } = isNullable;
    public string[] ErrorMessage { get; } = errorMessages;
}