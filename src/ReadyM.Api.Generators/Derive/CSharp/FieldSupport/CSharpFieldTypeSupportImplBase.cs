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
        else
        {
            EmitAutoApiMark(symbol, context);
        }
    }

    /// <summary>
    /// Emits, in a plain setter, a conditional API-flag set: when the thread opted in (server
    /// authoring scope), the write is treated as an authoritative override.
    /// </summary>
    protected virtual void EmitAutoApiMark(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (context.Model.MaskInfo == null)
            return;

        var bitExpr = $"({FullyQualifiedTypeName(context.Model.MaskInfo.Type)})1 << {context.Member.MaskIndex}";
        context.AppendLine($"if ({context.AutoMarkApiOnWriteVar})");
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
            context.AppendLine($"/// <inheritdoc cref=\"{context.State.CurrentVar}\"/>");
            context.AppendLine($"private {FullyQualifiedTypeName(symbol)} {context.Member.GeneratedPropertyName}");
            using (context.WithCodeBlock())
            {
                context.AppendLine("set");
                using (context.WithCodeBlock())
                {
                    EmitSetterBody(symbol, context, false);
                }
            }

            return;
        }

        var accessorType = FullyQualifiedTypeName(symbol);
        if (context.Member.AccessorSettings.BoolAccessors)
        {
            accessorType = "bool";
        }

        context.AppendLine($"/// <inheritdoc cref=\"{context.State.CurrentVar}\"/>");
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
                EmitSetterBody(symbol, context, false);
            }
        }

        var paramType = context.Member.AccessorSettings.BoolAccessors ? "bool" : FullyQualifiedTypeName(symbol);
        context.AppendLine($"private void {context.Member.GeneratedPropertyName}_SetFromApi({paramType} value, int id)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context, true);
        }
    }

    public void EmitNotifyChangesMethods(ITypeSymbol sourceType, CSharpEmitFieldSupportContext context)
    {
        context.AppendLine($"public void {context.Member.GeneratedPropertyName}NotifyChanged(int id)");
        using (context.WithCodeBlock())
        {
            context.EmitConflict.EmitNotifyChanged(sourceType, context.EmitConflictContext);
        }
    }

    protected virtual void EmitGetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (context.Member.AccessorSettings.BoolAccessors)
            context.AppendLine($"return {context.State.CurrentVar} != 0;");
        else
            context.AppendLine($"return {context.State.CurrentVar};");
    }

    protected virtual void EmitSetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool fromApi)
    {
        EmitSetterBodyInner(symbol, context, fromApi);

        // NOTE: We always reset time on API assignment, not just when the value differs. Nothing change can
        // still win with the client-side change

        // FIXME: There should be no fromApi check, however currently it's impossible to get hold of the current
        // entity in regular setters in order to have a lookup key
        if (fromApi)
            context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
    }

    protected virtual void EmitSetterBodyInner(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool fromApi)
    {
        context.Append("if ");
        EmitNotEqualCheck(symbol, context, forceParen: true);
        context.AppendLine();

        using (context.WithCodeBlock())
        {
            EmitAssign(symbol, context);
            EmitSetDirty(symbol, context, fromApi);
        }
    }

    public virtual void EmitFieldEnum(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var member = context.Member;
        var i = context.Member.MaskIndex;
        var maskType = context.Model.MaskInfo!.Type;
        var name = member.GeneratedPropertyName;
        var type = context.Member.AccessorSettings.BoolAccessors ? "bool" : member.Source.Type.ToString();
        var typeName = context.TypeName;

        context.AppendLine($"public static readonly Field<{typeName}, {type}> {name} = new({i},");
        context.AppendLine($"    static c => c.{name},");
        context.AppendLine($"    static (ref c, v) => c.{name} = v,");
        context.AppendLine($"    static (ref c, v, e) => c.{name}_SetFromApi(v, e),");
        context.AppendLine($"    static c => (c._apiMask & (({maskType})1 << {i})) != 0);");
    }

    public virtual void EmitSerializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        EmitSerializeBodyInner(symbol, context);
    }

    protected virtual void EmitSerializeBodyInner(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.EmitSerializeVar(context.State.CurrentVar, symbol);

    public void EmitDeserializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool resolveConflicts)
    {
        if (!resolveConflicts)
        {
            EmitDeserializeBodyInner(symbol, context, false);
            return;
        }

        context.Append("if (");
        context.EmitConflict.EmitCanChange(symbol, context.EmitConflictContext, forceParen: false);
        context.AppendLine(")");

        using (context.WithCodeBlock())
        {
            EmitDeserializeBodyInner(symbol, context, false);

            context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
        }

        context.AppendLine("else");

        using (context.WithCodeBlock())
        {
            EmitDeserializeBodyInner(symbol, context, true);
        }
    }

    protected virtual void EmitDeserializeBodyInner(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool skip)
    {
        var tempVar = context.MethodState.NewVarName("temp");
        using (context.WithCurrent(tempVar, symbol))
        {
            context.AppendLine($"{FullyQualifiedTypeName(symbol)} {tempVar} = default;");
            context.EmitDeserializeVar(context.State.CurrentVar, symbol);

            if (!skip)
            {
                if (context.Member.AccessorSettings.BoolAccessors)
                    context.AppendLine($"{context.Member.GeneratedPropertyName} = {tempVar} != 0;");
                else
                    context.AppendLine($"{context.Member.GeneratedPropertyName} = {tempVar};");
            }
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

    public virtual void EmitReadDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool resolveConflicts)
    {
        context.Append("if ");
        EmitDirtyCheck(symbol, context, forceParen: true);
        context.AppendLine();

        using (context.WithCodeBlock())
        {
            EmitDeserializeBody(symbol, context, resolveConflicts);
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
