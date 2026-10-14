using System;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

/// The network codec for a field marked [Interned]: strings go through InternedStringCodec, everything else is unchanged.
internal sealed class InternedNetDataSerializationCodec(ICSharpSerializationCodec inner) : ICSharpSerializationCodec
{
    private const string Codec = "global::ReadyM.Api.Multiplayer.Interning.InternedStringCodec";

    public void WriteNativeString(CSharpEmitSerializeContext context)
        => context.AppendLine($"{Codec}.Write(writer, in {context.State.CurrentVar});");

    public void ReadNativeString(CSharpEmitDeserializeContext context)
        => context.AppendLine($"{Codec}.Read(reader, ref {context.State.CurrentVar});");

    public void WritePrimitive(CSharpEmitSerializeContext context) => inner.WritePrimitive(context);
    public void ReadPrimitive(CSharpEmitDeserializeContext context) => inner.ReadPrimitive(context);

    public void WriteEnum(CSharpEmitSerializeContext context) => inner.WriteEnum(context);
    public void ReadEnum(CSharpEmitDeserializeContext context) => inner.ReadEnum(context);

    public void WriteSelf(CSharpEmitSerializeContext context) => inner.WriteSelf(context);
    public void ReadSelf(CSharpEmitDeserializeContext context) => inner.ReadSelf(context);

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
