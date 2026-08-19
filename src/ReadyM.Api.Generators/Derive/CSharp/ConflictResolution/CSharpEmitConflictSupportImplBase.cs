using System;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;

internal abstract class CSharpEmitConflictSupportImplBase : ICSharpEmitConflictSupportImpl
{
    protected virtual string EmitLastChanged(ITypeSymbol symbol, CSharpEmitConflictSupportContext context)
    {
        var lastChangedField = context.Member.GeneratedPropertyName + "LastChanged";
        return $"{context.EntityVarName}.GetComponent<ChangeComponent>().{lastChangedField}";
    }

    public virtual void EmitTryResolve(ITypeSymbol symbol, CSharpEmitConflictSupportContext context, bool forceParen)
    {
        if (forceParen)
            context.Append("(");

        context.Append($"{context.ResolveConflictsVar} && {context.LastObservedTimeVar} > {EmitLastChanged(symbol, context)}");

        if (forceParen)
            context.Append(")");
    }

    public virtual void EmitNotifyChange(ITypeSymbol symbol, CSharpEmitConflictSupportContext context)
    {
        context.AppendLine($"if ({context.ResolveConflictsVar})");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{EmitLastChanged(symbol, context)} = {context.LastObservedTimeVar};");
        }
    }
}
