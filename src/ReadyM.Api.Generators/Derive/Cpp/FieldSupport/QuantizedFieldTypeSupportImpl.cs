using System.Linq;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

/// A float or vector field marked [Quantize], [QuantizeAngle] or [HalfPrecision]. The native setter marks it dirty
/// only when it moves past the plain fields' cutoff and its encoding changes too, matching the managed side, so the
/// game does not resend values the wire cannot tell apart.
internal sealed class QuantizedFieldTypeSupportImpl : CppNonOverrideFieldTypeSupportImplBase
{
    private const string Include = "Interop/Quantization.h";

    public override bool Supports(DeriveMemberModel model)
        => model.Quantization is not null
           && model.QuantizationAttributeCount == 1
           && !model.Quantization.Problems(model.Source.Type).Any()
           && base.Supports(model);

    protected override bool Supports(ITypeSymbol type)
        => type.SpecialType == SpecialType.System_Single || SerializationHelper.IsVectorLike(type);

    public override void EmitAccessorMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool emitPublic)
    {
        context.State.ModuleState.AddInclude(Include, false);
        base.EmitAccessorMethods(symbol, context, emitPublic);
    }

    protected override void EmitGetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (symbol.SpecialType == SpecialType.System_Single)
            context.Append(CppTypeName(symbol));
        else
            base.EmitGetterType(symbol, context);
    }

    protected override void EmitSetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (symbol.SpecialType == SpecialType.System_Single)
            context.Append(CppTypeName(symbol));
        else
            base.EmitSetterType(symbol, context);
    }

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append(context.Member.Quantization!.CppUnchangedExpression(context.State.CurrentVar, "value"));

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append("!" + context.Member.Quantization!.CppUnchangedExpression(context.State.CurrentVar, "value"));
}
