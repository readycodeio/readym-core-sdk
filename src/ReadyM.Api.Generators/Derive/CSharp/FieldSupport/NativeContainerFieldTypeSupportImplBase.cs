using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal abstract class NativeContainerFieldTypeSupportImplBase : CSharpFieldTypeSupportImplBase
{
    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar}.Equals(value)");
    
    protected override void EmitAssign(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        // NOTE: No ownership transfer, this is copying the contents
        // Both the source and the destination have to be already allocated
        => context.AppendLine($"{context.State.CurrentVar}.Assign(value);");

    public override void EmitDeserializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var tempVar = context.ClassState.AddTempThreadStatic(symbol);
        
        using (context.WithCurrent(tempVar, symbol))
        {
            context.AppendLine($"{tempVar}.TryCreate(global::Yooni.Native.LowLevel.AllocatorKind.Default);");
            context.EmitDeserializeVar(tempVar, symbol);
            context.AppendLine($"{context.Member.GeneratedPropertyName} = {tempVar};");
        }
    }
    
    public override void EmitSkipDeltaBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var tempVar = context.ClassState.AddTempThreadStatic(symbol);

        context.Append("if ");
        EmitDirtyCheck(symbol, context, forceParen: true);
        context.AppendLine();
        
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{tempVar}.TryCreate(global::Yooni.Native.LowLevel.AllocatorKind.Default);");
            context.EmitDeserializeVar(tempVar, symbol);
        }
    }
    
    public override void EmitAssignComponentBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        context.AppendLine($"Set{context.Member.GeneratedPropertyName}(value.{context.State.CurrentVar});");
    }
}