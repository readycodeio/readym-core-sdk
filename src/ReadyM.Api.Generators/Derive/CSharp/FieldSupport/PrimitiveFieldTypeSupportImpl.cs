using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal sealed class PrimitiveFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsSerializablePrimitive(type.SpecialType);

    // Floats and doubles ignore a change within the epsilon, except a change from or to 0: a value set once into a
    // fresh field (like an item weight of 0.1) must arrive even when it is that small, and so must a reset to 0.
    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (symbol.SpecialType == SpecialType.System_Single)
            context.Append($"({context.State.CurrentVar} == 0) == (value == 0) && Math.Abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.ScalarComparisonEpsilon}f");
        else if (symbol.SpecialType == SpecialType.System_Double)
            context.Append($"({context.State.CurrentVar} == 0) == (value == 0) && Math.Abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.ScalarComparisonEpsilon}");
        else if (context.Member.AccessorSettings.BoolAccessors)
            context.Append($"({context.State.CurrentVar} != 0) == value");
        else
            context.Append($"{context.State.CurrentVar} == value");
    }

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (symbol.SpecialType == SpecialType.System_Single)
            context.Append($"({context.State.CurrentVar} == 0) != (value == 0) || Math.Abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.ScalarComparisonEpsilon}f");
        else if (symbol.SpecialType == SpecialType.System_Double)
            context.Append($"({context.State.CurrentVar} == 0) != (value == 0) || Math.Abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.ScalarComparisonEpsilon}");
        else if (context.Member.AccessorSettings.BoolAccessors)
            context.Append($"({context.State.CurrentVar} != 0) != value");
        else
            context.Append($"{context.State.CurrentVar} != value");
    }
}