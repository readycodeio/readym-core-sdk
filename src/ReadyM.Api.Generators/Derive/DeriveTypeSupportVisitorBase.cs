using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive;

internal class DeriveTypeSupportVisitorBase<TImpl>(IReadOnlyList<TImpl> impls, TImpl? fallbackImpl) 
    : DeriveSupportVisitorBase<ITypeSymbol, TImpl>(impls, fallbackImpl)
    where TImpl : IDeriveSupportImplBase<ITypeSymbol>
{
    protected override string ToDisplayString(ITypeSymbol item)
        => item.ToDisplayString();
}