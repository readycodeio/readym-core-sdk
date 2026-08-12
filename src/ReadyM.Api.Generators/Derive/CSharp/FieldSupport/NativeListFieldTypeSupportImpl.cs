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
            context.AppendLine($"Set{context.Member.GeneratedPropertyName}({tempVar});");
        }
    }
    
    public override void EmitAccessorMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (context.Member.AccessorSettings.SkipAccessors)
            return;

        if (!SerializationHelper.IsNativeList(symbol, out var itemType))
            throw new InvalidOperationException("Expected a native list type.");
        
        context.AppendLine($"public {FullyQualifiedTypeName(symbol)}.ReadOnly Get{context.Member.GeneratedPropertyName}()");
        using (context.WithCodeBlock())
        {
            EmitGetterBody(symbol, context);
        }
        context.AppendLine();
        
        context.AppendLine($"public void Set{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(symbol)} value)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context);
        }
        context.AppendLine();

        context.AppendLine($"public int {context.Member.GeneratedPropertyName}Count");
        using (context.WithCodeBlock())
        {
            context.AppendLine("get");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"return {context.State.CurrentVar}.Count;");
            }
        }
        context.AppendLine();

        context.AppendLine($"public {FullyQualifiedTypeName(itemType)} Get{context.Member.GeneratedPropertyName}(int index)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}[index];");
        }
        context.AppendLine();
        
        context.AppendLine($"public void Set{context.Member.GeneratedPropertyName}(int index, in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"if ({context.State.CurrentVar}.TrySet(index, value))");
            using (context.WithCodeBlock())
            {
                EmitSetDirty(symbol, context);
            }
        }
        context.AppendLine();
        
        context.AppendLine($"public bool Contains{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.Contains(value);");
        }
        context.AppendLine();
        
        context.AppendLine($"public void Add{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Add(value);");
            EmitSetDirty(symbol, context);
        }
        context.AppendLine();
        
        context.AppendLine($"public void Insert{context.Member.GeneratedPropertyName}(int index, in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Insert(index, value);");
            EmitSetDirty(symbol, context);
        }
        context.AppendLine();
        
        context.AppendLine($"public {FullyQualifiedTypeName(itemType)} RemoveAt{context.Member.GeneratedPropertyName}(int index)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"var result = {context.State.CurrentVar}.RemoveAt(index);");
            EmitSetDirty(symbol, context);
            context.AppendLine("return result;");
        }
        context.AppendLine();
        
        context.AppendLine($"public void Clear{context.Member.GeneratedPropertyName}()");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Clear();");
            EmitSetDirty(symbol, context);
        }
        
        context.AppendLine("/// <exclude />");
        context.AppendLine($"public void {context.Member.GeneratedPropertyName}_SetFromApi({FullyQualifiedTypeName(symbol)} value)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context, true);
        }
    }
    
    public override void EmitFieldEnum(ITypeSymbol sourceType, CSharpEmitFieldSupportContext context)
    {
        var member = context.Member;
        var i = context.Member.MaskIndex;
        var name = member.GeneratedPropertyName;
        var type = member.Source.Type;
        var typeName = context.Model.Source.Name;

        using (context.WithIndent())
        {
            context.AppendLine($"public static readonly Field<{typeName}, {type}> {name} = new({i},");
            context.AppendLine($"   static c => c.{context.State.CurrentVar},");
            context.AppendLine($"   static (ref c, v) => c.Set{context.Member.GeneratedPropertyName}(v),");
            context.AppendLine($"   static (ref c, v) => c.{name}_SetFromApi(v),");
            context.AppendLine($"   static c => c.Is{name}Dirty());");
        }
    }
}