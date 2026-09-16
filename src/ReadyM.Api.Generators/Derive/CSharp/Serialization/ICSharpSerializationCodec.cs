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

    /// <summary>A value type that serialises itself (custom method / save-serializable).</summary>
    void WriteSelf(CSharpEmitSerializeContext context);
    void ReadSelf(CSharpEmitDeserializeContext context);

    /// <summary>Frames a list and iterates its elements; <paramref name="emitElement"/> writes one element.</summary>
    void SerializeCollection(CSharpEmitSerializeContext context, string sourceVar, string iterVar, Action emitElement);

    /// <summary>Frames a list and loops; <paramref name="emitElement"/> reads and adds one element.</summary>
    void DeserializeCollection(CSharpEmitDeserializeContext context, string targetVar, Action emitElement);

    /// <summary>
    /// Frames a dictionary. The codec declares the key/value locals from <paramref name="iterVar"/> and calls the
    /// emit callbacks; a keyed format can wrap each entry so its reader advances one entry at a time.
    /// </summary>
    void SerializeDictionary(
        CSharpEmitSerializeContext context, string sourceVar, string iterVar,
        string keyVar, string valueVar, Action emitKey, Action emitValue);

    void DeserializeDictionary(
        CSharpEmitDeserializeContext context, string targetVar,
        string keyVar, string keyTypeFqn, Action emitKeyRead,
        string valueVar, string valueTypeFqn, Action emitValueRead);
}
