using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal abstract class CppFieldTypeSupportImplBase : ICppFieldTypeSupportImpl
{
    public abstract bool Supports(ITypeSymbol type);

    protected virtual void EmitGetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.Append("const ");
        context.Append(CppTypeName(symbol));
        context.Append("&");
    }

    protected virtual void EmitSetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.Append("const ");
        context.Append(CppTypeName(symbol));
        context.Append("&");
    }

    protected virtual void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool forceParen)
    {
        using (context.WithExpr(forceParen))
        {
            EmitEqualCheck(symbol, context);
        }
    }

    protected abstract void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context);

    protected virtual void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool forceParen)
    {
        using (context.WithExpr(forceParen))
        {
            EmitNotEqualCheck(symbol, context);
        }
    }
    
    protected virtual void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.Append("!");
        EmitEqualCheck(symbol, context, forceParen: true);
    }

    protected virtual void EmitSetDirty(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.AppendLine($"{context.CurrentMaskVar} |= static_cast<{CppTypeName(context.MaskType)}>(1) << {context.MaskIndex};");

    protected virtual void EmitAssign(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.AppendLine($"{context.State.CurrentVar} = value;");
    }
    
    public virtual void EmitGetterMethod(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        EmitGetterType(symbol, context);
        context.AppendLine($" {context.State.GeneratedPropertyName}() const");
        using (context.WithCodeBlock())
        {
            EmitGetterBody(symbol, context);
        }
    }

    public virtual void EmitSetterMethod(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.Append("void Set");
        context.Append(context.State.GeneratedPropertyName);
        context.Append("(");
        EmitSetterType(symbol, context);
        context.AppendLine(" value)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context);
        }
    }

    public virtual void EmitGetterBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.AppendLine($"return {context.State.CurrentVar};");

    public virtual void EmitSetterBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.Append("if ");
        EmitNotEqualCheck(symbol, context, forceParen: true);
        context.AppendLine();
        
        using (context.WithCodeBlock())
        {
            EmitAssign(symbol, context);
            EmitSetDirty(symbol, context);
        }
    }
}