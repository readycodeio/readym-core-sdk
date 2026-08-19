using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;

internal interface ICSharpEmitConflictSupportImpl
{
    void EmitTryResolve(ITypeSymbol symbol, CSharpEmitConflictSupportContext context, bool forceParen);
    void EmitNotifyChange(ITypeSymbol symbol, CSharpEmitConflictSupportContext context);
}
