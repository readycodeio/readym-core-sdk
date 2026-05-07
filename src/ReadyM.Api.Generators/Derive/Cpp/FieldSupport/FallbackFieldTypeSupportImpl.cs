using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal class FallbackFieldTypeSupportImpl : CppFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => true;

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append("true /* invalid field */");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append("false /* invalid field */");

    protected override void EmitSetDirty(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.AppendLine("/* invalid field, cannot set dirty */");

    public override void EmitGetterBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.AppendLine("throw std::logic_error(\"Unsupported field type\");");

    public override void EmitSetterBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.AppendLine("throw std::logic_error(\"Unsupported field type\");");
}