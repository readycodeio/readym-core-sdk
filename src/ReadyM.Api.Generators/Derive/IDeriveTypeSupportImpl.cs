using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive;

internal interface IDeriveTypeSupportImpl<in TContext> : IDeriveTypeSupportImplBase
{
    public void Visit(ITypeSymbol symbol, TContext context);
}