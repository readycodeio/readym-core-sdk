using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive;

internal interface IDeriveTypeSupportImplBase
{
    public abstract bool Supports(ITypeSymbol type);
}