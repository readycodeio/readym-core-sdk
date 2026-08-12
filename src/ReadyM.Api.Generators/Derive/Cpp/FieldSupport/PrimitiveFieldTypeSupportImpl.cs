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

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.State.CurrentType.SpecialType == SpecialType.System_Single)
            context.Append($"std::abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.ScalarComparisonEpsilon}f");
        else if (context.State.CurrentType.SpecialType == SpecialType.System_Double)
            context.Append($"std::abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.ScalarComparisonEpsilon}");
        else if (context.Member.AccessorSettings.BoolAccessors)
            context.Append($"({context.State.CurrentVar} != 0) == value");
        else
            context.Append($"{context.State.CurrentVar} == value");
    }
    
    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.State.CurrentType.SpecialType == SpecialType.System_Single)
            context.Append($"std::abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.ScalarComparisonEpsilon}f");
        else if (context.State.CurrentType.SpecialType == SpecialType.System_Double)
            context.Append($"std::abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.ScalarComparisonEpsilon}");
        else if (context.Member.AccessorSettings.BoolAccessors)
            context.Append($"({context.State.CurrentVar} != 0) != value");
        else
            context.Append($"{context.State.CurrentVar} != value");
    }
}