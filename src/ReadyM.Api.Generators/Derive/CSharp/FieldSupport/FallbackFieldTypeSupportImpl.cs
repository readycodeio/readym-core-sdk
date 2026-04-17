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

    protected override void EmitGetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append($"throw new System.NotSupportedException(\"No support for field type {symbol.ToDisplayString()}\");");

    protected override void EmitSetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append($"throw new System.NotSupportedException(\"No support for field type {symbol.ToDisplayString()}\");");
}