using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class NativeStringFieldTypeSupportImpl : CppFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeString(type, out _);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} == value");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} != value");
}