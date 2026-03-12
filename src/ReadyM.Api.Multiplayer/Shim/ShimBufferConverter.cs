using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Shim;

internal class ShimBufferConverter : JsonConverter<ShimBuffer>
{
    public override ShimBuffer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new ShimBuffer();
        
        DebugJson.Assert(reader.TokenType == JsonTokenType.String);
        var str = reader.GetString();
        if (str == null)
            return new ShimBuffer();
        
        var bytes = Convert.FromBase64String(str);
        return new ShimBuffer(bytes);
    }

    public override void Write(Utf8JsonWriter writer, ShimBuffer value, JsonSerializerOptions options)
    {
        if (value.Data == null)
        {
            writer.WriteNullValue();
            return;
        }
        
        var str = Convert.ToBase64String(value.Data, value.Offset, value.MaxSize);
        writer.WriteStringValue(str);
    }
}