using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;

internal abstract class CSharpEmitConflictSupportImplBase : ICSharpEmitConflictSupportImpl
{
    protected virtual string EmitLastChanged(ITypeSymbol symbol, CSharpEmitConflictSupportContext context, bool nullable)
    {
        var lastChangedField = context.Member.GeneratedPropertyName + "LastChanged";
        return $"({context.ChangeStoreVar}{(nullable ? "?" : "")}.GetChangeComponent<ChangeComponent>({context.IdentVarName}).{lastChangedField}{(nullable ? " ?? 0" : "")})";
    }

    public void EmitCanChange(ITypeSymbol symbol, CSharpEmitConflictSupportContext context, bool forceParen)
    {
        if (forceParen)
            context.Append("(");

        context.Append($"!({context.LastObservedTimeVar} < {EmitLastChanged(symbol, context, true)})");

        if (forceParen)
            context.Append(")");
    }

    public virtual void EmitNotifyChanged(ITypeSymbol symbol, CSharpEmitConflictSupportContext context)
    {
        context.AppendLine($"if ({context.ChangeStoreVar} != null)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{EmitLastChanged(symbol, context, false)} = {context.LastObservedTimeVar};");
        }
    }
}
