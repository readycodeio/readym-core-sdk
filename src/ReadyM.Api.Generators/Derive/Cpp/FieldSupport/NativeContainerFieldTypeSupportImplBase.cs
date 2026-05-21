using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal abstract class NativeContainerFieldTypeSupportImplBase : CppNonOverrideFieldTypeSupportImplBase
{
    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} == value");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} != value");
    
    protected override void EmitAssign(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.AppendLine($"{context.State.CurrentVar}.Assign(value);");

    public override void EmitAssignComponentBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.AppendLine($"Set{context.Member.GeneratedPropertyName}(value.{context.State.CurrentVar});");
    
    public override bool HasCreate(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => true;

    public override void EmitTryCreateBody(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (HasCreate(symbol, context))
        {
            context.AppendLine($"{context.State.CurrentVar}.TryCreate(allocatorKind);");
        }
    }
}