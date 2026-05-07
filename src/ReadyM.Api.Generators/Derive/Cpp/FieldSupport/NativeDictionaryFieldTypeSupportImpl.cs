using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal class NativeDictionaryFieldTypeSupportImpl : NativeContainerFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeDictionary(type, out _, out _, out _);

    public override void EmitAccessorMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (!SerializationHelper.IsNativeDictionary(symbol, out var keyType, out var valueType, out _))
            throw new InvalidOperationException("Expected a native dictionary type");
        
        base.EmitAccessorMethods(symbol, context);
        
        context.AppendLine($"int {context.Member.GeneratedPropertyName}Count() const");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.GetCount();");
        }
        context.AppendLine();

        if (SerializationHelper.IsPrimitive(valueType))
        {
            context.AppendLine($"{CppTypeName(valueType)} Get{context.Member.GeneratedPropertyName}(const {CppTypeName(keyType)}& key) const");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"return {context.State.CurrentVar}.GetItem(key);");
            }
        }
        else
        {
            context.AppendLine($"const {CppTypeName(valueType)}& Get{context.Member.GeneratedPropertyName}(const {CppTypeName(keyType)}& key) const");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"return {context.State.CurrentVar}.GetItemRef(key);");
            }
        }
        context.AppendLine();
        
        context.AppendLine($"void Set{context.Member.GeneratedPropertyName}(const {CppTypeName(keyType)}& key, const {CppTypeName(valueType)}& value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.SetItem(key, value);");
            EmitSetDirty(symbol, context);
        }
        context.AppendLine();
        
        context.AppendLine($"bool Contains{context.Member.GeneratedPropertyName}Key(const {CppTypeName(keyType)}& key) const");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.ContainsKey(key);");
        }
        context.AppendLine();

        context.AppendLine($"bool Contains{context.Member.GeneratedPropertyName}(const {CppTypeName(keyType)}& key, const {CppTypeName(valueType)}& value) const");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.Contains(key, value);");
        }
        context.AppendLine();

        context.AppendLine($"std::optional<{CppTypeName(valueType)}> TryGet{context.Member.GeneratedPropertyName}Value(const {CppTypeName(keyType)}& key) const");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"return {context.State.CurrentVar}.TryGetValue(key);");
        }
        context.AppendLine();

        context.AppendLine($"bool Add{context.Member.GeneratedPropertyName}(const {CppTypeName(keyType)}& key, const {CppTypeName(valueType)}& value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"auto result = {context.State.CurrentVar}.Add(key, value);");
            EmitSetDirty(symbol, context);
            context.AppendLine("return result;");
        }
        context.AppendLine();

        context.AppendLine($"void Clear{context.Member.GeneratedPropertyName}()");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Clear();");
            EmitSetDirty(symbol, context);
        }
        context.AppendLine();

        context.AppendLine($"bool Remove{context.Member.GeneratedPropertyName}(const {CppTypeName(keyType)}& key)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"auto result = {context.State.CurrentVar}.Remove(key);");
            EmitSetDirty(symbol, context);
            context.AppendLine("return result;");
        }
        context.AppendLine();
    }
}