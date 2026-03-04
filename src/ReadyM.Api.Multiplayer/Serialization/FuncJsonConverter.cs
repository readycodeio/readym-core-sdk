using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReadyM.Api.Multiplayer.Serialization;

public class FuncJsonConverter<T>(TextSerializeMethod<T> serializeFunc, TextDeserializeMethod<T> deserializeFunc) : JsonConverter<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => deserializeFunc.Invoke(ref reader, options);

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => serializeFunc.Invoke(writer, value, options);
}