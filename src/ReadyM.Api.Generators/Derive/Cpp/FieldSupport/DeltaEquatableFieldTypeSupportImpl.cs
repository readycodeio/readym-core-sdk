using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class DeltaEquatableFieldTypeSupportImpl : CppFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsDeltaEquatable(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar}.DeltaEquals(value, {DeriveComponentUtils.ScalarComparisonEpsilon}f)");
}