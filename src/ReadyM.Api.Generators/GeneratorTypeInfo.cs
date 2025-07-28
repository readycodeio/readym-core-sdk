using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal class GeneratorTypeInfo
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public (string Name, ITypeSymbol Type, int Order)[] Fields { get; set; }
    public bool IsNullable { get; set; }
    public bool UseCons { get; set; }
}