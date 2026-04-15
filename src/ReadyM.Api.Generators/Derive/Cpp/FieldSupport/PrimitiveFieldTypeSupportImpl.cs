using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class PrimitiveFieldTypeSupportImpl : CppFieldTypeSupportImplBase
{
    protected override void EmitGetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append(CppTypeName(symbol));

    protected override void EmitSetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append(CppTypeName(symbol));

    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsSerializablePrimitive(type.SpecialType);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.State.CurrentType.SpecialType == SpecialType.System_Single)
            context.Append($"std::abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.FloatComparisonEpsilon}f");
        else if (context.State.CurrentType.SpecialType == SpecialType.System_Double)
            context.Append($"std::abs({context.State.CurrentVar} - value) <= {DeriveComponentUtils.DoubleComparisonEpsilon}");
        else
            context.Append($"{context.State.CurrentVar} == value");
    }
    
    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.State.CurrentType.SpecialType == SpecialType.System_Single)
            context.Append($"std::abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.FloatComparisonEpsilon}f");
        else if (context.State.CurrentType.SpecialType == SpecialType.System_Double)
            context.Append($"std::abs({context.State.CurrentVar} - value) > {DeriveComponentUtils.DoubleComparisonEpsilon}");
        else
            context.Append($"{context.State.CurrentVar} != value");
    }
}