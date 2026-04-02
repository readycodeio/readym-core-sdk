using System.Text.Json;

namespace ReadyM.Api.Multiplayer.Serialization;

internal delegate void TextSerializeMethod(Utf8JsonWriter writer, object customObject, JsonSerializerOptions options);
internal delegate void TextSerializeMethod<in T>(Utf8JsonWriter writer, T customObject, JsonSerializerOptions options);