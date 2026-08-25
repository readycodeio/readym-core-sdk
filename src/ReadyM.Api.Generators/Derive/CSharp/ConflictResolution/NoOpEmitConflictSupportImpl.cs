using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;

internal class NoOpEmitConflictSupportImplBase : ICSharpEmitConflictSupportImpl
{
    public void EmitCanChange(ITypeSymbol symbol, CSharpEmitConflictSupportContext context, bool forceParen)
        => context.Append("true");

    public void EmitNotifyChanged(ITypeSymbol symbol, CSharpEmitConflictSupportContext context)
    {
        // empty
    }
}
