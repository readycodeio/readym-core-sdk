using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal abstract class CppFieldTypeSupportImplBase : ICppFieldTypeSupportImpl
{
    public abstract bool Supports(DeriveMemberModel type);

    protected virtual void EmitGetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.Settings.BoolAccessors)
        {
            context.Append("bool");
        }
        else
        {
            context.Append("const ");
            context.Append(CppTypeName(symbol));
            context.Append("&");
        }
    }

    protected virtual void EmitSetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.Settings.BoolAccessors)
        {
            context.Append("bool");
        }
        else
        {
            context.Append("const ");
            context.Append(CppTypeName(symbol));
            context.Append("&");
        }
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
    
    protected virtual void EmitDirtyCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool forceParen)
    {
        if (context.Model.MaskInfo == null)
            throw new InvalidOperationException();
        
        using (context.WithExpr(forceParen))
        {
            context.Append($"({context.CurrentMaskVar} & (static_cast<{CppTypeName(context.Model.MaskInfo.Type)}>(1) << {context.Member.MaskIndex})) != 0");
        }
    }
    
    protected virtual void EmitSetDirty(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Model.MaskInfo == null)
            return;
        var maskType = context.Model.MaskInfo.Type;
        context.AppendLine($"{context.CurrentMaskVar} |= static_cast<{CppTypeName(maskType)}>(1) << {context.Member.MaskIndex};");
    }

    protected virtual void EmitAssign(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.Settings.BoolAccessors)
            context.AppendLine($"{context.State.CurrentVar} = value ? 1 : 0;");
        else
            context.AppendLine($"{context.State.CurrentVar} = value;");
    }

    public virtual void EmitDirtyMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.AppendLine($"bool Is{context.Member.GeneratedPropertyName}Dirty() const");
        using (context.WithCodeBlock())
        {
            context.Append($"return ");
            EmitDirtyCheck(symbol, context, false);
            context.AppendLine(";");
        }
        context.AppendLine();
        
        context.Append($"void Set{context.Member.GeneratedPropertyName}Dirty()");
        using (context.WithCodeBlock())
        {
            EmitSetDirty(symbol, context);
        }
        context.AppendLine();
    }

    public virtual void EmitAccessorMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool emitPublic)
    {
        if (emitPublic)
        {
            if (context.Member.Settings.SkipAccessors)
                return;
            
            EmitGetterMethod(symbol, context, true);
            context.AppendLine();
            
            if (!context.Member.Source.ReadOnly)
            {
                EmitSetterMethod(symbol, context, true);
                context.AppendLine();
            }
        }
        else // emitPrivate
        {
            if (!context.Member.Settings.SkipAccessors)
                return;
            
            EmitGetterMethod(symbol, context, false);
            context.AppendLine();

            if (!context.Member.Source.ReadOnly)
            {
                EmitSetterMethod(symbol, context, false);
                context.AppendLine();
            }
        }
    }

    public virtual void EmitGetterMethod(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool isPublic)
    {
        if (!isPublic)
            context.Append("/* private */"); // NOTE: Used inside tests
        EmitGetterType(symbol, context);
        context.Append($" {context.Member.GeneratedPropertyName}() const");
        context.AppendLine();
        using (context.WithCodeBlock())
        {
            EmitGetterBody(symbol, context);
        }
    }

    public virtual void EmitSetterMethod(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool isPublic)
    {
        if (!isPublic)
            context.Append("/* private */"); // NOTE: Used inside tests
        context.Append("void Set");
        context.Append(context.Member.GeneratedPropertyName);
        context.Append("(");
        EmitSetterType(symbol, context);
        context.AppendLine(" value)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context);
        }
    }

    public virtual void EmitGetterBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.Settings.BoolAccessors)
            context.AppendLine($"return {context.State.CurrentVar} != 0;");
        else
            context.AppendLine($"return {context.State.CurrentVar};");
    }

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

    public virtual bool HasCreate(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => false;

    public virtual void EmitTryCreateBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        // empty
    }

    public virtual bool HasDispose(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => symbol.AllInterfaces.Any(i => i.ContainingNamespace.ToDisplayString() == "System" && i.Name == "IDisposable");

    public virtual void EmitDisposeBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (HasDispose(symbol, context))
        {
            if (symbol.IsReferenceType)
                context.AppendLine($"{context.State.CurrentVar}?.Dispose();");
            else
                context.AppendLine($"{context.State.CurrentVar}.Dispose();");
        }
    }

    public virtual bool HasAssignComponent(ITypeSymbol sourceType, CppEmitFieldSupportContext context)
        => true;

    public virtual void EmitAssignComponentBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.AppendLine($"Set{context.Member.GeneratedPropertyName}(value.{context.Member.GeneratedPropertyName}());");
    }

    public virtual void EmitBackingField(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.AppendLine($"{CppTypeName(symbol)} {context.Member.Source.Name} = {GetCppDefaultValue(symbol)};");
    }
}