using System.Text.Json;

namespace ReadyM.Api.Multiplayer.Serialization;

public delegate object? TextDeserializeMethod(ref Utf8JsonReader reader, JsonSerializerOptions options);
public delegate T? TextDeserializeMethod<out T>(ref Utf8JsonReader reader, JsonSerializerOptions options);