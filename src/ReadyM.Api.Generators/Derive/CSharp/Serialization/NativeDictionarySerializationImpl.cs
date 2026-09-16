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

        var sourceVar = context.State.CurrentVar;
        var itemVar = context.MethodState.NewVarName("d");
        var keyVar = context.MethodState.NewVarName("key");
        var valueVar = context.MethodState.NewVarName("value");
        context.Codec.SerializeDictionary(context, sourceVar, itemVar, keyVar, valueVar,
            () => context.EmitSerializeVar(keyVar, keyType),
            () => context.EmitSerializeVar(valueVar, valueType));
    }

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
    {
        if (!SerializationHelper.IsNativeDictionary(symbol, out var keyType, out var valueType, out _))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native dictionary type");

        var targetVar = context.State.CurrentVar;
        var keyVar = context.MethodState.NewVarName("key");
        var valueVar = context.MethodState.NewVarName("value");
        context.Codec.DeserializeDictionary(context, targetVar,
            keyVar, FullyQualifiedTypeName(keyType), () => context.EmitDeserializeVar(keyVar, keyType),
            valueVar, FullyQualifiedTypeName(valueType), () => context.EmitDeserializeVar(valueVar, valueType));
    }
}
