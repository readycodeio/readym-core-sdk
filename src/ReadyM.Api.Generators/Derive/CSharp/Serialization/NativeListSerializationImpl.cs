using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class NativeListSerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeList(type, out _);

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
    {
        if (!SerializationHelper.IsNativeList(symbol, out var itemType))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native list type");

        var itemVar = context.State.NewVarName("item");
        context.AppendLine($"writer.Put({context.State.CurrentVar}.Count);");
        context.AppendLine($"foreach (var {itemVar} in {context.State.CurrentVar})");
        using (context.WithCodeBlock())
        {
            context.EmitSerializeVar(itemVar, itemType);
        }
    }

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
    {
        if (!SerializationHelper.IsNativeList(symbol, out var itemType))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native list type");
        
        var indexVar = context.State.NewVarName("index");
        var countVar = context.State.NewVarName("count");
        context.AppendLine($"var {countVar} = reader.GetInt();");
        context.AppendLine($"{context.State.CurrentVar}.Clear();");
        context.AppendLine($"for (var {indexVar} = 0; {indexVar} < {countVar}; {indexVar}++)");
        using (context.WithCodeBlock())
        {
            var itemVar = context.State.NewVarName("item");
            context.AppendLine($"var {itemVar} = default({FullyQualifiedTypeName(itemType)});");
            context.EmitDeserializeVar(itemVar, itemType);
            context.AppendLine($"{context.State.CurrentVar}.Add({itemVar});");
        }
    }
}