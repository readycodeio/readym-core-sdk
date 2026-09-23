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

        var sourceVar = context.State.CurrentVar;
        var itemVar = context.MethodState.NewVarName("item");
        context.Codec.SerializeCollection(context, sourceVar, itemVar,
            () => context.EmitSerializeVar(itemVar, itemType));
    }

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
    {
        if (!SerializationHelper.IsNativeList(symbol, out var itemType))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native list type");

        var targetVar = context.State.CurrentVar;
        context.Codec.DeserializeCollection(context, targetVar, () =>
        {
            var itemVar = context.MethodState.NewVarName("item");
            context.AppendLine($"var {itemVar} = default({FullyQualifiedTypeName(itemType)});");
            context.EmitDeserializeVar(itemVar, itemType);
            context.AppendLine($"{targetVar}.Add({itemVar});");
        });
    }
}
