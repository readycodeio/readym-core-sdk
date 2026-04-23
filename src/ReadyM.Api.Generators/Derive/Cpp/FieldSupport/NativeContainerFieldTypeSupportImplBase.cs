using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal abstract class NativeContainerFieldTypeSupportImplBase : CppFieldTypeSupportImplBase
{
    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} == value");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} != value");
    
    protected override void EmitAssign(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.AppendLine($"{context.State.CurrentVar}.Assign(value);");
}