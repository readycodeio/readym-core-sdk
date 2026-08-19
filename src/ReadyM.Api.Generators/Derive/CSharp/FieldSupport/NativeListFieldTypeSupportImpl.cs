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

                // FIXME: There should be a check, but it's currently not possible to get an entity in regular setters
                if (false)
                    context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
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

            // FIXME: There should be a check, but it's currently not possible to get an entity in regular setters
            if (false)
                context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
        }
        context.AppendLine();

        context.AppendLine($"public void Insert{context.Member.GeneratedPropertyName}(int index, in {FullyQualifiedTypeName(itemType)} value)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"{context.State.CurrentVar}.Insert(index, value);");
            EmitSetDirty(symbol, context);

            // FIXME: There should be a check, but it's currently not possible to get an entity in regular setters
            if (false)
                context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
        }
        context.AppendLine();

        context.AppendLine($"public {FullyQualifiedTypeName(itemType)} RemoveAt{context.Member.GeneratedPropertyName}(int index)");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"var result = {context.State.CurrentVar}.RemoveAt(index);");
            EmitSetDirty(symbol, context);

            // FIXME: There should be a check, but it's currently not possible to get an entity in regular setters
            if (false)
                context.EmitConflict.EmitNotifyChanged(symbol, context.EmitConflictContext);
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

        context.AppendLine("/// <exclude />");
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
        var typeName = context.TypeName;

        context.AppendLine($"public static readonly Field<{typeName}, {type}> {name} = new({i},");
        context.AppendLine($"    static c => c.{context.State.CurrentVar},");
        context.AppendLine($"    static (ref c, v) => c.Set{context.Member.GeneratedPropertyName}(v),");
        context.AppendLine($"    static (ref c, v, e) => c.{name}_SetFromApi(v, e),");
        context.AppendLine($"    static c => c.Is{name}Dirty());");
    }
}
