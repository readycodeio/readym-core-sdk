using System;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

/// <summary>
/// Seam between the shared value walk and the target format. The serialization strategies decide structure
/// (which types are leaves, how a collection recurses); the codec decides the concrete read/write calls and the collection framing.
/// </summary>
internal interface ICSharpSerializationCodec
{
    void WritePrimitive(CSharpEmitSerializeContext context);
    void ReadPrimitive(CSharpEmitDeserializeContext context);

    void WriteEnum(CSharpEmitSerializeContext context);
    void ReadEnum(CSharpEmitDeserializeContext context);

    void WriteNativeString(CSharpEmitSerializeContext context);
    void ReadNativeString(CSharpEmitDeserializeContext context);

    void WriteSelf(CSharpEmitSerializeContext context);
    void ReadSelf(CSharpEmitDeserializeContext context);

    /// <summary>Frames a collection and iterates its elements; <paramref name="emitElement"/> writes one element.</summary>
    void SerializeCollection(CSharpEmitSerializeContext context, string sourceVar, string iterVar, Action emitElement);

    /// <summary>Frames a collection and loops; <paramref name="emitElement"/> reads and adds one element.</summary>
    void DeserializeCollection(CSharpEmitDeserializeContext context, string targetVar, Action emitElement);
}
