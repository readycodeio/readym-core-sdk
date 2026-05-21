using System.Collections.Generic;

namespace ReadyM.Api.Generators.Derive;

internal class DeriveFieldSupportVisitor<TImpl, TContext>(IReadOnlyList<TImpl> impls, TImpl? fallbackImpl) 
    : DeriveFieldSupportVisitorBase<TImpl>(impls, fallbackImpl), IDeriveSupportVisitor<DeriveMemberModel, TContext>
    where TImpl : IDeriveSupportImpl<DeriveMemberModel, TContext>
{
    public void Visit(DeriveMemberModel symbol, TContext context)
    {
        var impl = GetImpl(symbol, true);
        
        impl.Visit(symbol, context);
    }
}