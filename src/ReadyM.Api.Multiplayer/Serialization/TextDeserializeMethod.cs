using System.Text.Json;

namespace ReadyM.Api.Multiplayer.Serialization;

internal delegate object? TextDeserializeMethod(ref Utf8JsonReader reader, JsonSerializerOptions options);
internal delegate T? TextDeserializeMethod<out T>(ref Utf8JsonReader reader, JsonSerializerOptions options);