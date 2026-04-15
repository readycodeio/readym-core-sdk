using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class NativeContainerFieldTypeSupportImpl : CppFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeContainer(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} == value");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} != value");
    
    protected override void EmitAssign(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (SerializationHelper.IsNativeString(symbol, out _))
            base.EmitAssign(symbol, context);
        else
            context.AppendLine($"{context.State.CurrentVar}.Assign(value);");
    }
}