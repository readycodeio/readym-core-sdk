using System;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class NativeStringSerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeString(type, out _);

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
    {
        if (!SerializationHelper.IsNativeString(symbol, out _))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native string type");

        context.Codec.WriteNativeString(context);
    }

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
    {
        if (!SerializationHelper.IsNativeString(symbol, out _))
            throw new InvalidOperationException($"Type {symbol.ToDisplayString()} is not a supported native string type");

        context.Codec.ReadNativeString(context);
    }
}
