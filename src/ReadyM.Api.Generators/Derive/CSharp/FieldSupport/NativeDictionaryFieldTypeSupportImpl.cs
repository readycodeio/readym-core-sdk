using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class NativeDictionaryFieldTypeSupportImpl : NativeContainerFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeDictionary(type, out _, out _, out _);

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
        if (context.Member.Settings.SkipAccessors)
            return;

        if (!SerializationHelper.IsNativeDictionary(symbol, out var keyType, out var valueType, out _))
            throw new InvalidOperationException("Expected a native dictionary type");

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

        context.AppendLine($"public {FullyQualifiedTypeName(valueType)} Get{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(keyType)} key)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}[in key];");
        }

        context.AppendLine();
        
        context.AppendLine($"public void Set{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(keyType)} key, in {FullyQualifiedTypeName(valueType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"if ({context.State.CurrentVar}.TrySet(in key, value))");
            using (context.WithCodeBlock())
            {
                EmitSetDirty(symbol, context);
            }
        }

        context.AppendLine();

        context.AppendLine($"public bool Contains{context.Member.GeneratedPropertyName}Key(in {FullyQualifiedTypeName(keyType)} key)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.ContainsKey(in key);");
        }

        context.AppendLine();

        context.AppendLine($"public bool Contains{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(keyType)} key, in {FullyQualifiedTypeName(valueType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.Contains(in key, value);");
        }

        context.AppendLine();

        context.AppendLine($"public bool TryGet{context.Member.GeneratedPropertyName}Value(in {FullyQualifiedTypeName(keyType)} key, out {FullyQualifiedTypeName(valueType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.TryGetValue(in key, out value);");
        }

        context.AppendLine();
        
        context.AppendLine($"public bool Add{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(keyType)} key, in {FullyQualifiedTypeName(valueType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"var result = {context.State.CurrentVar}.Add(in key, value);");
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

        context.AppendLine();

        context.AppendLine($"public bool Remove{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(keyType)} key)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"var result = {context.State.CurrentVar}.Remove(in key);");
            EmitSetDirty(symbol, context);
            context.AppendLine("return result;");
        }

        context.AppendLine();

        context.AppendLine($"public void {context.State.GeneratedPropertyName}_SetFromApi({FullyQualifiedTypeName(symbol)} value)");
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
        var fieldName = member.Source.Name;

        using (context.WithIndent())
        {
            context.AppendLine($"public static readonly Field<{typeName}, {type}> {name} = new({i},");
            context.AppendLine($"   static c => c.{fieldName},");
            context.AppendLine($"   static (ref c, v) => c.Set{context.State.GeneratedPropertyName}(v),");
            context.AppendLine($"   static (ref c, v) => c.{name}_SetFromApi(v),");
            context.AppendLine($"   static c => ((c._apiMask >> {i}) & 0x1f) == 1);");
        }
    }
}