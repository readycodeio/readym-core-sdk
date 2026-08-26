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

    protected override void EmitDeserializeBodyInner(ITypeSymbol symbol, CSharpEmitFieldSupportContext context, bool skip)
    {
        var tempVar = context.ClassState.AddTempThreadStatic(symbol);

        using (context.WithCurrent(tempVar, symbol))
        {
            context.AppendLine($"{tempVar}.TryCreate(global::Yooni.Native.LowLevel.AllocatorKind.Default);");
            context.EmitDeserializeVar(tempVar, symbol);
            if (!skip)
                context.AppendLine($"Set{context.Member.GeneratedPropertyName}({tempVar});");
        }
    }

    public override void EmitAccessorMethods(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (context.Member.AccessorSettings.SkipAccessors)
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
            EmitSetterBody(symbol, context, false);
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

                // FIXME: There should be a check, but it's currently not possible to get an entity in regular setters
                if (false)
                    context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
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
            using (context.WithCodeBlock())
            {
                EmitSetDirty(symbol, context);

                // FIXME: There should be a check, but it's currently not possible to get an entity in regular setters
                if (false)
                    context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
            }
            context.AppendLine("return result;");
        }

        context.AppendLine();

        context.AppendLine($"public void Clear{context.Member.GeneratedPropertyName}()");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Clear();");
            EmitSetDirty(symbol, context);

            // FIXME: There should be a check, but it's currently not possible to get an entity in regular setters
            if (false)
                context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
        }

        context.AppendLine();

        context.AppendLine($"public bool Remove{context.Member.GeneratedPropertyName}(in {FullyQualifiedTypeName(keyType)} key)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"var result = {context.State.CurrentVar}.Remove(in key);");
            using (context.WithCodeBlock())
            {
                EmitSetDirty(symbol, context);

                // FIXME: There should be a check, but it's currently not possible to get an entity in regular setters
                if (false)
                    context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
            }
            context.AppendLine("return result;");
        }

        context.AppendLine();

        context.AppendLine($"public void {context.Member.GeneratedPropertyName}_SetFromApi({FullyQualifiedTypeName(symbol)} value, int id)");
        using (context.WithCodeBlock())
        {
            EmitSetterBody(symbol, context, true);
        }
    }

    public override void EmitFieldEnum(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var member = context.Member;
        var i = context.Member.MaskIndex;
        var name = member.GeneratedPropertyName;
        var type = member.Source.Type;
        var fieldName = member.Source.Name;
        var typeName = context.TypeName;

        context.AppendLine($"public static readonly Field<{typeName}, {type}> {name} = new({i},");
        context.AppendLine($"    static c => c.{fieldName},");
        context.AppendLine($"    static (ref c, v) => c.Set{context.Member.GeneratedPropertyName}(v),");
        context.AppendLine($"    static (ref c, v, e) => c.{name}_SetFromApi(v, e),");
        context.AppendLine($"    static c => c.Is{name}Dirty());");
    }
}
