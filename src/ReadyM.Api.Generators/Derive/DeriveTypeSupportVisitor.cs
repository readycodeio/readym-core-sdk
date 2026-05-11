using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive;

internal class DeriveTypeSupportVisitor<TImpl, TContext>(IReadOnlyList<TImpl> impls, TImpl? fallbackImpl) 
    : DeriveTypeSupportVisitorBase<TImpl>(impls, fallbackImpl), IDeriveTypeSupportVisitor<TContext>
    where TImpl : IDeriveTypeSupportImpl<TContext>
{
    public void Visit(ITypeSymbol symbol, TContext context)
    {
        var impl = GetImpl(symbol, true);
        
        impl.Visit(symbol, context);
    }
}