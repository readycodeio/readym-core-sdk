using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Serialization;

internal class PolymorphicNullableObjectJsonConverter(TextRelaySerializer serializer) : JsonConverter<object?>
{
    private TextRelaySerializer _serializer { get; } = serializer;
        
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DebugJson.Assert(reader.TokenType == JsonTokenType.StartArray);
        reader.Read();
            
        var discriminator = reader.GetString();

        object? result = null;
        if (discriminator != null)
        {
            if (!_serializer.PolymorphicByDiscriminator.TryGetValue(discriminator, out var dataType))
                throw new JsonException($"Unknown discriminator: {discriminator}");

            reader.Read();
            result = JsonSerializer.Deserialize(ref reader, dataType, options);
        }
        else
        {
            reader.Read();
            reader.Skip();
        }

        reader.Read();
        DebugJson.Assert(reader.TokenType == JsonTokenType.EndArray, "Expected end of array after object value");
        return result;
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        if (value == null)
        {
            writer.WriteNullValue();
            writer.WriteNullValue();
        }
        else
        {
            var dataType = value.GetType();
            if (!_serializer.PolymorphicByType.TryGetValue(dataType, out var discriminator))
                throw new JsonException($"Unknown type: {dataType}");
    
            writer.WriteStringValue(discriminator);
            JsonSerializer.Serialize(writer, value, dataType, options);
        }
            
        writer.WriteEndArray();
    }
}