using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal abstract class NativeContainerFieldTypeSupportImplBase : CSharpFieldTypeSupportImplBase
{
    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar}.IsCreated && {context.State.CurrentVar}.Equals(value)");

    protected override void EmitAssign(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        // NOTE: No ownership transfer, this is copying the contents
        context.AppendLine($"{context.State.CurrentVar}.TryCreate(global::Yooni.Native.LowLevel.AllocatorKind.Default);");
        context.AppendLine($"{context.State.CurrentVar}.Assign(value);");
    }

    protected override void EmitDeserializeBodyInner(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool skip)
    {
        var tempVar = context.ClassState.AddTempThreadStatic(symbol);

        using (context.WithCurrent(tempVar, symbol))
        {
            context.AppendLine($"{tempVar}.TryCreate(global::Yooni.Native.LowLevel.AllocatorKind.Default);");
            context.EmitDeserializeVar(tempVar, symbol);
            if (!skip)
            {
                context.AppendLine($"{context.Member.GeneratedPropertyName}.TryCreate(global::Yooni.Native.LowLevel.AllocatorKind.Default);");
                context.AppendLine($"{context.Member.GeneratedPropertyName}.Assign({tempVar});");
            }
        }
    }

    public override void EmitAssignComponentBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.AppendLine($"Set{context.Member.GeneratedPropertyName}(value.{context.State.CurrentVar});");
    }
}
