using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;

internal interface ICSharpEmitConflictSupportImpl
{
    void EmitCanChange(ITypeSymbol symbol, CSharpEmitConflictSupportContext context, bool forceParen);
    void EmitNotifyChanged(ITypeSymbol symbol, CSharpEmitConflictSupportContext context);
}
