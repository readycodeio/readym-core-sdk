using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal abstract class CSharpFieldTypeSupportImplBase : ICSharpFieldTypeSupportImpl
{
    public abstract bool Supports(ITypeSymbol type);

    protected virtual void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool forceParen)
    {
        using (context.WithExpr(forceParen))
        {
            EmitEqualCheck(symbol, context);
        }
    }
    
    protected abstract void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context);

    protected virtual void EmitNotEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool forceParen)
    {
        using (context.WithExpr(forceParen))
        {
            EmitNotEqualCheck(symbol, context);
        }
    }

    protected virtual void EmitNotEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.Append("!");
        EmitEqualCheck(symbol, context, forceParen: true);
    }

    protected virtual void EmitDirtyCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool forceParen)
    {
        using (context.WithExpr(forceParen))
        {
            context.Append($"(mask & (({context.MaskType})1 << {context.MaskIndex})) != 0");
        }
    }

    public virtual void EmitSetDirty(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.AppendLine($"{context.CurrentMaskVar} |= ({FullyQualifiedTypeName(context.MaskType)})1 << {context.MaskIndex};");

    public virtual void EmitGetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.AppendLine($"return {context.State.CurrentVar};");

    public virtual void EmitSetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.Append("if ");
        EmitNotEqualCheck(symbol, context, forceParen: true);
        context.AppendLine();
        
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar} = value;");
            EmitSetDirty(symbol, context);
        }
    }

    public virtual void EmitSerializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.EmitSerializeVar(context.State.CurrentVar, symbol);

    public virtual void EmitDeserializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var tempVar = context.State.NewVarName("temp");
        using (context.WithCodeBlock())
        using (context.WithCurrent(tempVar, symbol))
        {
            context.AppendLine($"{FullyQualifiedTypeName(symbol)} {tempVar} = default;");
            context.EmitDeserializeVar(context.State.CurrentVar, symbol);
            context.AppendLine($"{context.State.GeneratedPropertyName} = {tempVar};");
        }
    }

    public virtual void EmitWriteDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.AppendLine($"if ");
        EmitDirtyCheck(symbol, context, forceParen: true);
        context.AppendLine();
        
        using (context.WithIndent())
        {
            EmitSerializeBody(symbol, context);
        }
    }

    public virtual void EmitReadDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.Append($"if ");
        EmitDirtyCheck(symbol, context, forceParen: true);
        context.AppendLine();
        
        using (context.WithIndent())
        {
            EmitDeserializeBody(symbol, context);
        }
    }

    public virtual void EmitSkipDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.Append($"if ");
        EmitDirtyCheck(symbol, context, forceParen: true);
        context.AppendLine();
        
        using (context.WithCodeBlock())
        {
            var dummyVar = context.State.NewVarName("dummy");
            context.AppendLine($"var {dummyVar} = default({FullyQualifiedTypeName(context.State.CurrentType)});");
            context.EmitDeserializeVar(dummyVar, context.State.CurrentType);
        }
    }
}