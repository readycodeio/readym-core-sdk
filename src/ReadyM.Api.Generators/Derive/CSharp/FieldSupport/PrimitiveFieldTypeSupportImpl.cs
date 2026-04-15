using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal sealed class PrimitiveFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsSerializablePrimitive(type.SpecialType);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (symbol.SpecialType == SpecialType.System_Single)
            context.Append($"Math.Abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.FloatComparisonEpsilon}f");
        else if (symbol.SpecialType == SpecialType.System_Double)
            context.Append($"Math.Abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.DoubleComparisonEpsilon}");
        else
            context.Append($"{context.State.CurrentVar} == value");
    }

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (symbol.SpecialType == SpecialType.System_Single)
            context.Append($"Math.Abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.FloatComparisonEpsilon}f");
        else if (symbol.SpecialType == SpecialType.System_Double)
            context.Append($"Math.Abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.DoubleComparisonEpsilon}");
        else
            context.Append($"{context.State.CurrentVar} != value");
    }
}