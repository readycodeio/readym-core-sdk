using System.Collections.Generic;

namespace ReadyM.Api.Generators.Derive;

internal class DeriveFieldSupportVisitorBase<TImpl>(IReadOnlyList<TImpl> impls, TImpl? fallbackImpl) 
    : DeriveSupportVisitorBase<DeriveMemberModel, TImpl>(impls, fallbackImpl)
    where TImpl : IDeriveSupportImplBase<DeriveMemberModel>
{
    protected override string ToDisplayString(DeriveMemberModel item)
        => item.Source.Symbol.ToDisplayString();
}