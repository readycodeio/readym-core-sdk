using System.Text.Json;

namespace ReadyM.Api.Multiplayer.Serialization;

public delegate void TextSerializeMethod(Utf8JsonWriter writer, object customObject, JsonSerializerOptions options);
public delegate void TextSerializeMethod<in T>(Utf8JsonWriter writer, T customObject, JsonSerializerOptions options);