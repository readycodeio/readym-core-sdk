using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class NativeDictionarySerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeDictionary(type, out _, out _, out _);

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
    {
        if (!SerializationHelper.IsNativeDictionary(symbol, out var keyType, out var valueType, out _))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native dictionary type");

        var itemVar = context.State.NewVarName("d");
        var keyVar = context.State.NewVarName("key");
        var valueVar = context.State.NewVarName("value");
        context.AppendLine($"writer.Put({context.State.CurrentVar}.Count);");
        context.AppendLine($"foreach (var {itemVar} in {context.State.CurrentVar})");
        using (context.WithCodeBlock())
        {
            context.AppendLine($"var {keyVar} = {itemVar}.Key;");
            context.AppendLine($"var {valueVar} = {itemVar}.Value;");
            context.EmitSerializeVar(keyVar, keyType);
            context.EmitSerializeVar(valueVar, valueType);
        }
    }

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
    {
        if (!SerializationHelper.IsNativeDictionary(symbol, out var keyType, out var valueType, out _))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native dictionary type");
        
        var indexVar = context.State.NewVarName("index");
        var countVar = context.State.NewVarName("count");
        context.AppendLine($"var {countVar} = reader.GetInt();");
        context.AppendLine($"{context.State.CurrentVar}.Clear();");
        context.AppendLine($"for (var {indexVar} = 0; {indexVar} < {countVar}; {indexVar}++)");
        using (context.WithCodeBlock())
        {
            var keyVar = context.State.NewVarName("key");
            var valueVar = context.State.NewVarName("value");
            context.AppendLine($"var {keyVar} = default({FullyQualifiedTypeName(keyType)});");
            context.AppendLine($"var {valueVar} = default({FullyQualifiedTypeName(valueType)});");
            context.EmitDeserializeVar(keyVar, keyType);
            context.EmitDeserializeVar(valueVar, valueType);
            context.AppendLine($"{context.State.CurrentVar}.Add({keyVar}, {valueVar});");
        }
    }
}