using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive;

internal interface IDeriveTypeSupportVisitor<in TContext>
{
    public void Visit(ITypeSymbol symbol, TContext context);
}