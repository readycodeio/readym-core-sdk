using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal class NativeListFieldTypeSupportImpl : NativeContainerFieldTypeSupportImplBase
{
    protected override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeList(type, out _);

    public override void EmitAccessorMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool emitPublic)
    {
        if (!SerializationHelper.IsNativeList(symbol, out var itemType))
            throw new InvalidOperationException("Expected a native list type");
        
        base.EmitAccessorMethods(symbol, context, emitPublic);

        if (emitPublic)
        {
            if (context.Member.AccessorSettings.SkipAccessors)
                return;

            context.AppendLine($"int {context.Member.GeneratedPropertyName}Count() const");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"return {context.State.CurrentVar}.GetCount();");
            }
            context.AppendLine();
            
            context.AppendLine($"const {CppTypeName(itemType)}& Get{context.Member.GeneratedPropertyName}(int index) const");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"return {context.State.CurrentVar}[index];");
            }
            context.AppendLine();
            
            context.AppendLine($"void Set{context.Member.GeneratedPropertyName}(int index, const {CppTypeName(itemType)}& value)");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"{context.State.CurrentVar}[index] = value;");
                EmitSetDirty(symbol, context);
            }
            context.AppendLine();
            
            context.AppendLine($"bool Contains{context.Member.GeneratedPropertyName}(const {CppTypeName(itemType)}& value) const");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"return {context.State.CurrentVar}.Contains(value);");
            }
            context.AppendLine();

            context.AppendLine($"void Add{context.Member.GeneratedPropertyName}(const {CppTypeName(itemType)}& value)");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"{context.State.CurrentVar}.Add(value);");
                EmitSetDirty(symbol, context);
            }
            context.AppendLine();
            
            context.AppendLine($"void Insert{context.Member.GeneratedPropertyName}(int index, const {CppTypeName(itemType)}& value)");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"{context.State.CurrentVar}.Insert(index, value);");
                EmitSetDirty(symbol, context);
            }
            context.AppendLine();

            context.AppendLine($"void RemoveAt{context.Member.GeneratedPropertyName}(int index)");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"{context.State.CurrentVar}.RemoveAt(index);");
                EmitSetDirty(symbol, context);
            }
            context.AppendLine();

            context.AppendLine($"void Clear{context.Member.GeneratedPropertyName}()");
            using (context.WithCodeBlock())
            {
                context.AppendLine($"{context.State.CurrentVar}.Clear();");
                EmitSetDirty(symbol, context);
            }
            context.AppendLine();            
        }
    }
}