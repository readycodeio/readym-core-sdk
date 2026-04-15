using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public static class DeriveCSharpUtils
{
    public static string FullyQualifiedTypeName(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}