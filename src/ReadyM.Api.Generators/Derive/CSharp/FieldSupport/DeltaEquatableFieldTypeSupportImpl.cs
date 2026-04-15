using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class DeltaEquatableFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsDeltaEquatable(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (symbol.IsValueType)
            context.Append($"{context.State.CurrentVar}.DeltaEquals(value, {DeriveComponentUtils.FloatComparisonEpsilon}f)");
        else
            context.Append($"{context.State.CurrentVar}?.DeltaEquals(value, {DeriveComponentUtils.FloatComparisonEpsilon}f) ?? value is null");
    }
}