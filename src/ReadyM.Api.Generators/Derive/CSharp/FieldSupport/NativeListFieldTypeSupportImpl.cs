using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class NativeListFieldTypeSupportImpl : NativeContainerFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeList(type, out _);

    protected override void EmitGetterBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.AppendLine($"return {context.State.CurrentVar}.AsReadOnly();");
    
    public override void EmitDeserializeBody(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var tempVar = context.ClassState.AddTempThreadStatic(symbol);
        
        using (context.WithCurrent(tempVar, symbol))
        {
            context.AppendLine($"{tempVar}.TryCreate(global::Yooni.Native.LowLevel.AllocatorKind.Default);");
            context.EmitDeserializeVar(tempVar, symbol);
            context.AppendLine($"Set{context.State.GeneratedPropertyName}({tempVar});");
        }
    }
    
    public override void EmitAccessorMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (!SerializationHelper.IsNativeList(symbol, out var itemType))
            throw new InvalidOperationException("Expected a native list type.");
        
        context.AppendLine($"public {FullyQualifiedTypeName(symbol)}.ReadOnly Get{context.State.GeneratedPropertyName}()");
        using (context.WithCodeBlock())
        {
            EmitGetterBody(symbol, context);
        }
        context.AppendLine();
        
        context.AppendLine($"public void Set{context.State.GeneratedPropertyName}(in {FullyQualifiedTypeName(symbol)} value)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context);
        }
        context.AppendLine();

        context.AppendLine($"public int {context.State.GeneratedPropertyName}Count");
        using (context.WithCodeBlock())
        {
            context.AppendLine("get");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"return {context.State.CurrentVar}.Count;");
            }
        }
        context.AppendLine();

        context.AppendLine($"public {FullyQualifiedTypeName(itemType)} Get{context.State.GeneratedPropertyName}(int index)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}[index];");
        }
        context.AppendLine();
        
        context.AppendLine($"public void Set{context.State.GeneratedPropertyName}(int index, in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"if ({context.State.CurrentVar}.TrySet(index, value))");
            using (context.WithCodeBlock())
            {
                EmitSetDirty(symbol, context);
            }
        }
        context.AppendLine();
        
        context.AppendLine($"public bool Contains{context.State.GeneratedPropertyName}(in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.Contains(value);");
        }
        context.AppendLine();
        
        context.AppendLine($"public void Add{context.State.GeneratedPropertyName}(in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Add(value);");
            EmitSetDirty(symbol, context);
        }
        context.AppendLine();
        
        context.AppendLine($"public void Insert{context.State.GeneratedPropertyName}(int index, in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Insert(index, value);");
            EmitSetDirty(symbol, context);
        }
        context.AppendLine();
        
        context.AppendLine($"public {FullyQualifiedTypeName(itemType)} RemoveAt{context.State.GeneratedPropertyName}(int index)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"var result = {context.State.CurrentVar}.RemoveAt(index);");
            EmitSetDirty(symbol, context);
            context.AppendLine("return result;");
        }
        context.AppendLine();
        
        context.AppendLine($"public void Clear{context.State.GeneratedPropertyName}()");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Clear();");
            EmitSetDirty(symbol, context);
        }
    }
}