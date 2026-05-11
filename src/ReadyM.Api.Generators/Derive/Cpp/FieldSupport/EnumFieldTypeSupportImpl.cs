using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class EnumFieldTypeSupportImpl : CppNonOverrideFieldTypeSupportImplBase
{
    protected override void EmitGetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append(CppTypeName(symbol));

    protected override void EmitSetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append(CppTypeName(symbol));

    protected override bool Supports(ITypeSymbol type)
        => type.TypeKind == TypeKind.Enum;

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} == value");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} != value");
}