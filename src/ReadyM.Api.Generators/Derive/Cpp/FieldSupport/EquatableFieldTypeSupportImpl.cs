using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class EquatableFieldTypeSupportImpl : CppNonOverrideFieldTypeSupportImplBase
{
    protected override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsEquatable(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} == value");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} != value");
}