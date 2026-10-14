using System;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

/// The network codec for a field marked [Quantize], [QuantizeAngle] or [HalfPrecision]: floats and vectors go through
/// the field's encoding, everything else is unchanged.
internal sealed class QuantizedNetDataSerializationCodec(ICSharpSerializationCodec inner, DeriveQuantization quantization)
    : ICSharpSerializationCodec
{
    public void WritePrimitive(CSharpEmitSerializeContext context)
        => context.AppendLine(quantization.WriteStatement(context.State.CurrentVar));

    public void ReadPrimitive(CSharpEmitDeserializeContext context)
        => context.AppendLine($"{context.State.CurrentVar} = {quantization.ReadExpression(context.State.CurrentType)};");

    // Vectors serialise themselves in the plain codec, so this is where a quantized vector is written
    public void WriteSelf(CSharpEmitSerializeContext context)
        => context.AppendLine(quantization.WriteStatement(context.State.CurrentVar));

    public void ReadSelf(CSharpEmitDeserializeContext context)
        => context.AppendLine($"{context.State.CurrentVar} = {quantization.ReadExpression(context.State.CurrentType)};");

    public void WriteEnum(CSharpEmitSerializeContext context) => inner.WriteEnum(context);
    public void ReadEnum(CSharpEmitDeserializeContext context) => inner.ReadEnum(context);

    public void WriteNativeString(CSharpEmitSerializeContext context) => inner.WriteNativeString(context);
    public void ReadNativeString(CSharpEmitDeserializeContext context) => inner.ReadNativeString(context);

    public void WriteEntityRef(CSharpEmitSerializeContext context) => inner.WriteEntityRef(context);
    public void ReadEntityRef(CSharpEmitDeserializeContext context) => inner.ReadEntityRef(context);

    public void SerializeCollection(CSharpEmitSerializeContext context, string sourceVar, string iterVar, Action emitElement)
        => inner.SerializeCollection(context, sourceVar, iterVar, emitElement);

    public void DeserializeCollection(CSharpEmitDeserializeContext context, string targetVar, Action emitElement)
        => inner.DeserializeCollection(context, targetVar, emitElement);

    public void SerializeDictionary(
        CSharpEmitSerializeContext context, string sourceVar, string iterVar,
        string keyVar, string valueVar, Action emitKey, Action emitValue)
        => inner.SerializeDictionary(context, sourceVar, iterVar, keyVar, valueVar, emitKey, emitValue);

    public void DeserializeDictionary(
        CSharpEmitDeserializeContext context, string targetVar,
        string keyVar, string keyTypeFqn, Action emitKeyRead,
        string valueVar, string valueTypeFqn, Action emitValueRead)
        => inner.DeserializeDictionary(context, targetVar, keyVar, keyTypeFqn, emitKeyRead, valueVar, valueTypeFqn, emitValueRead);
}
