using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;

internal class NoOpEmitConflictSupportImplBase : ICSharpEmitConflictSupportImpl
{
    public void EmitTryResolve(ITypeSymbol symbol, CSharpEmitConflictSupportContext context, bool forceParen)
        => context.Append("true");

    public void EmitNotifyChange(ITypeSymbol symbol, CSharpEmitConflictSupportContext context)
    {
        // empty
    }
}
