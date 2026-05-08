using System;
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
            context.Append($"(mask & (({context.Model.MaskInfo.Type})1 << {context.Member.MaskIndex})) != 0");
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

    protected virtual void EmitAssign(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.AppendLine($"{context.State.CurrentVar} = value;");

    protected virtual void EmitGetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.AppendLine($"return {context.State.CurrentVar};");

    protected virtual void EmitSetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool fromApi = false)
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

    public virtual void EmitAccessorMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.AppendLine($"public {FullyQualifiedTypeName(symbol)} {context.State.GeneratedPropertyName}");
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

        context.AppendLine($"private void {context.State.GeneratedPropertyName}_SetFromApi({FullyQualifiedTypeName(symbol)} value)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context, true);
        }
    }

    public virtual void EmitFieldEnum(ITypeSymbol sourceType, CSharpEmitFieldSupportContext context)
    {
        var member = context.Member;
        var i = context.Member.MaskIndex;
        var name = member.GeneratedPropertyName;
        var type = member.Source.Type;
        var typeName = context.Model.Source.Name;
        var fieldName = member.Source.Name;

        using (context.WithIndent())
        {
            context.AppendLine($"public static readonly Field<{typeName}, {type}> {name} = new({i},");
            context.AppendLine($"   static c => c.{fieldName},");
            context.AppendLine($"   static (ref c, v) => c.{name} = v,");
            context.AppendLine($"   static (ref c, v) => c.{name}_SetFromApi(v),");
            context.AppendLine($"   static c => ((c._apiMask >> {i}) & 0x1f) == 1);");
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
            context.AppendLine($"{context.State.GeneratedPropertyName} = {tempVar};");
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

    public virtual void EmitSkipDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.Append("if ");
        EmitDirtyCheck(symbol, context, forceParen: true);
        context.AppendLine();

        using (context.WithCodeBlock())
        {
            var dummyVar = context.MethodState.NewVarName("dummy");
            context.AppendLine($"var {dummyVar} = default({FullyQualifiedTypeName(context.State.CurrentType)});");
            context.EmitDeserializeVar(dummyVar, context.State.CurrentType);
        }
    }
}