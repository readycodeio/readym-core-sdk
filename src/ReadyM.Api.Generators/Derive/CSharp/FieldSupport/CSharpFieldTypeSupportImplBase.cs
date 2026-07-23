using System;
using System.Linq;
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
        if (context.Model.MaskInfo == null)
            throw new InvalidOperationException();

        using (context.WithExpr(forceParen))
        {
            context.Append($"({context.CurrentMaskVar} & (({context.Model.MaskInfo.Type})1 << {context.Member.MaskIndex})) != 0");
        }
    }

    protected virtual void EmitSetDirty(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool fromApi = false)
    {
        if (context.Model.MaskInfo == null)
            return;

        var bitExpr = $"({FullyQualifiedTypeName(context.Model.MaskInfo.Type)})1 << {context.Member.MaskIndex}";
        context.AppendLine($"{context.CurrentMaskVar} |= {bitExpr};");

        if (fromApi)
        {
            context.AppendLine($"{context.CurrentApiMaskVar} |= {bitExpr};");
        }
    }

    /// <summary>
    /// Emits a conditional API-flag set for a plain (non-API) setter. When the writing thread has
    /// opted in (the server ECS thread), an ordinary write is treated as an authoritative override,
    /// so it gets replicated back to the entity's owner without an explicit MarkChangedFromApi() call.
    /// </summary>
    protected virtual void EmitAutoApiMark(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (context.Model.MaskInfo == null)
            return;

        var bitExpr = $"({FullyQualifiedTypeName(context.Model.MaskInfo.Type)})1 << {context.Member.MaskIndex}";
        context.AppendLine("if (global::ReadyM.Api.Multiplayer.ComponentWriteContext.AutoMarkApiOnWrite)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.CurrentApiMaskVar} |= {bitExpr};");
        }
    }

    protected virtual void EmitAssign(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (context.Member.AccessorSettings.BoolAccessors)
            context.AppendLine($"{context.State.CurrentVar} = (byte)(value ? 1 : 0);");
        else
            context.AppendLine($"{context.State.CurrentVar} = value;");
    }

    public void EmitDirtyMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.AppendLine($"private bool Is{context.Member.GeneratedPropertyName}Dirty()");
        using (context.WithCodeBlock())
        {
            context.Append($"return ");
            EmitDirtyCheck(symbol, context, false);
            context.AppendLine(";");
        }

        context.AppendLine();

        context.Append($"private void Set{context.Member.GeneratedPropertyName}Dirty()");
        using (context.WithCodeBlock())
        {
            EmitSetDirty(symbol, context);
        }

        context.AppendLine();
    }

    public virtual void EmitAccessorMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (context.Member.AccessorSettings.SkipAccessors)
        {
            context.AppendLine($"private {FullyQualifiedTypeName(symbol)} {context.Member.GeneratedPropertyName}");
            using (context.WithCodeBlock())
            {
                context.AppendLine("set");
                using (context.WithCodeBlock())
                {
                    EmitSetterBody(symbol, context);
                }
            }

            return;
        }

        var accessorType = FullyQualifiedTypeName(symbol);
        if (context.Member.AccessorSettings.BoolAccessors)
        {
            accessorType = "bool";
        }

        context.AppendLine($"public {accessorType} {context.Member.GeneratedPropertyName}");
        using (context.WithCodeBlock())
        {
            context.AppendLine("get");
            using (context.WithCodeBlock())
            {
                EmitGetterBody(symbol, context);
            }

            context.AppendLine("set");
            using (context.WithCodeBlock())
            {
                EmitSetterBody(symbol, context);
            }
        }

        var paramType = context.Member.AccessorSettings.BoolAccessors ? "bool" : FullyQualifiedTypeName(symbol);
        context.AppendLine($"private void {context.Member.GeneratedPropertyName}_SetFromApi({paramType} value)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context, true);
        }
    }

    protected virtual void EmitGetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (context.Member.AccessorSettings.BoolAccessors)
            context.AppendLine($"return {context.State.CurrentVar} != 0;");
        else
            context.AppendLine($"return {context.State.CurrentVar};");
    }

    protected virtual void EmitSetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool fromApi = false)
    {
        context.Append("if ");
        EmitNotEqualCheck(symbol, context, forceParen: true);
        context.AppendLine();

        using (context.WithCodeBlock())
        {
            EmitAssign(symbol, context);
            EmitSetDirty(symbol, context, fromApi);

            if (!fromApi)
                EmitAutoApiMark(symbol, context);
        }
    }

    public virtual void EmitFieldEnum(ITypeSymbol sourceType, CSharpEmitFieldSupportContext context)
    {
        var member = context.Member;
        var i = context.Member.MaskIndex;
        var name = member.GeneratedPropertyName;
        var type = context.Member.AccessorSettings.BoolAccessors ? "bool" : member.Source.Type.ToString();
        var typeName = context.Model.Source.Name;

        using (context.WithIndent())
        {
            context.AppendLine($"public static readonly Field<{typeName}, {type}> {name} = new({i},");
            context.AppendLine($"   static c => c.{name},");
            context.AppendLine($"   static (ref c, v) => c.{name} = v,");
            context.AppendLine($"   static (ref c, v) => c.{name}_SetFromApi(v),");
            context.AppendLine($"   static c => c.Is{name}Dirty());");
        }
    }

    public virtual void EmitSerializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.EmitSerializeVar(context.State.CurrentVar, symbol);

    public virtual void EmitDeserializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var tempVar = context.MethodState.NewVarName("temp");
        using (context.WithCurrent(tempVar, symbol))
        {
            context.AppendLine($"{FullyQualifiedTypeName(symbol)} {tempVar} = default;");
            context.EmitDeserializeVar(context.State.CurrentVar, symbol);

            if (context.Member.AccessorSettings.BoolAccessors)
                context.AppendLine($"{context.Member.GeneratedPropertyName} = {tempVar} != 0;");
            else
                context.AppendLine($"{context.Member.GeneratedPropertyName} = {tempVar};");
        }
    }

    public virtual void EmitWriteDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.Append("if ");
        EmitDirtyCheck(symbol, context, forceParen: true);
        context.AppendLine();

        using (context.WithCodeBlock())
        {
            EmitSerializeBody(symbol, context);
        }
    }

    public virtual void EmitReadDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.Append("if ");
        EmitDirtyCheck(symbol, context, forceParen: true);
        context.AppendLine();

        using (context.WithCodeBlock())
        {
            EmitDeserializeBody(symbol, context);
        }
    }

    public virtual bool HasDispose(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        return symbol.AllInterfaces.Any(i => i.ContainingNamespace.ToDisplayString() == "System" && i.Name == "IDisposable");
    }

    public virtual void EmitDisposeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (HasDispose(symbol, context))
        {
            if (symbol.IsReferenceType)
                context.AppendLine($"{context.State.CurrentVar}?.Dispose();");
            else
                context.AppendLine($"{context.State.CurrentVar}.Dispose();");
        }
    }

    public virtual void EmitAssignComponentBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.AppendLine($"{context.Member.GeneratedPropertyName} = value.{context.Member.GeneratedPropertyName};");
    }
}