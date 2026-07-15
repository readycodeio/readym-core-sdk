using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadyM.Api.Idents;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Shim;

internal class ShimRecordingJsonConverter : JsonConverter<ShimRecording>
{
    public override ShimRecording Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DebugJson.Assert(reader.TokenType == JsonTokenType.StartObject);

        var playerId = (PlayerId?) null;
        var items = new List<ShimResponseItem>();
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            DebugJson.Assert(reader.TokenType == JsonTokenType.PropertyName);
            var propertyName = reader.GetString();
            reader.Read();

            if (propertyName == "playerId")
            {
                var playerRawValue = reader.GetInt32();
                playerId = playerRawValue >= 0 ? new PlayerId((ushort)playerRawValue) : null;
            }
            else if (propertyName == "items")
            {
                items = JsonSerializer.Deserialize<List<ShimResponseItem>>(ref reader, options);
            }
            else
            {
                reader.Skip();
            }
        }
        
        return new ShimRecording(items!, playerId);
    }

    public override void Write(Utf8JsonWriter writer, ShimRecording value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("playerId");
        var playerRawValue = value.PlayerId?.RawValue ?? -1;
        writer.WriteNumberValue(playerRawValue);
        writer.WritePropertyName("items");
        JsonSerializer.Serialize(writer, value.ResponseItems, options);
        writer.WriteEndObject();
    }
}