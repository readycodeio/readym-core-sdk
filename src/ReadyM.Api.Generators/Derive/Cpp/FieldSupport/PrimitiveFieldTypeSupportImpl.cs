using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class PrimitiveFieldTypeSupportImpl : CppNonOverrideFieldTypeSupportImplBase
{
    protected override void EmitGetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.AccessorSettings.BoolAccessors)
            context.Append("bool");
        else
            context.Append(CppTypeName(symbol));
    }

    protected override void EmitSetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.AccessorSettings.BoolAccessors)
            context.Append("bool");
        else
            context.Append(CppTypeName(symbol));
    }

    protected override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsSerializablePrimitive(type.SpecialType);

    // Floats and doubles ignore a change within the epsilon, except a change from or to 0: a value set once into a
    // fresh field (like an item weight of 0.1) must arrive even when it is that small, and so must a reset to 0.
    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.State.CurrentType.SpecialType == SpecialType.System_Single)
            context.Append($"({context.State.CurrentVar} == 0) == (value == 0) && std::abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.ScalarComparisonEpsilon}f");
        else if (context.State.CurrentType.SpecialType == SpecialType.System_Double)
            context.Append($"({context.State.CurrentVar} == 0) == (value == 0) && std::abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.ScalarComparisonEpsilon}");
        else if (context.Member.AccessorSettings.BoolAccessors)
            context.Append($"({context.State.CurrentVar} != 0) == value");
        else
            context.Append($"{context.State.CurrentVar} == value");
    }
    
    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.State.CurrentType.SpecialType == SpecialType.System_Single)
            context.Append($"({context.State.CurrentVar} == 0) != (value == 0) || std::abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.ScalarComparisonEpsilon}f");
        else if (context.State.CurrentType.SpecialType == SpecialType.System_Double)
            context.Append($"({context.State.CurrentVar} == 0) != (value == 0) || std::abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.ScalarComparisonEpsilon}");
        else if (context.Member.AccessorSettings.BoolAccessors)
            context.Append($"({context.State.CurrentVar} != 0) != value");
        else
            context.Append($"{context.State.CurrentVar} != value");
    }
}