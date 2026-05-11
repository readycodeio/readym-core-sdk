using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class NativeStringSerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeString(type, out _);

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
    {
        if (!SerializationHelper.IsNativeString(symbol, out var size))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native list type");

        context.State.ModuleState.AddUsing("Yooni.Native.Serialization");
        context.AppendLine($"{context.State.CurrentVar}.Serialize(writer);");
    }

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
    {
        if (!SerializationHelper.IsNativeString(symbol, out var size))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native list type");

        context.State.ModuleState.AddUsing("Yooni.Native.Serialization");
        context.AppendLine($"{context.State.CurrentVar}.Deserialize(reader);");
    }
}