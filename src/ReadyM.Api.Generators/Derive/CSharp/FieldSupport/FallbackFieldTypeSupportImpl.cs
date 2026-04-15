using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class FallbackFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => true;

    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append("true /* invalid field */");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append("false /* invalid field */");

    public override void EmitGetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.AppendLine($"throw new System.InvalidOperationException(\"Field type {symbol.ToDisplayString()} is not supported\");");

    public override void EmitSetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.AppendLine($"throw new System.InvalidOperationException(\"Field type {symbol.ToDisplayString()} is not supported\");");
}