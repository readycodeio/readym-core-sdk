using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadyM.Api.Idents;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Serialization;

public class DefaultTextRelaySerializerRegistration : ITextRelaySerializerRegistration
{
    public void Register(TextRelaySerializer serializer)
    {
        serializer.RegisterPolymorphicType<byte>(
            "byte", 
            (writer, value, options) => { writer.WriteNumberValue(value); },
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.Number, "Expected number value for byte");
                return reader.GetByte();
            }
        );

        serializer.RegisterPolymorphicType<short>(
            "short",
            (writer, value, options) => { writer.WriteNumberValue(value); },
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.Number, "Expected number value for short");
                return reader.GetInt16();
            }
        );

        serializer.RegisterPolymorphicType<int>(
            "int",
            (writer, value, options) => { writer.WriteNumberValue(value); },
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.Number, "Expected number value for int");
                return reader.GetInt32();
            }
        );

        serializer.RegisterPolymorphicType<long>(
            "long",
            (writer, value, options) => { writer.WriteNumberValue(value); },
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.Number, "Expected number value for long");
                return reader.GetInt64();
            }
        );

        serializer.RegisterPolymorphicType<float>(
            "float",
            (writer, value, options) => { writer.WriteNumberValue(value); },
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.Number, "Expected number value for float");
                return reader.GetSingle();
            }
        );

        serializer.RegisterPolymorphicType<double>(
            "double",
            (writer, value, options) => { writer.WriteNumberValue(value); },
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.Number, "Expected number value for double");
                return reader.GetDouble();
            }
        );

        serializer.RegisterPolymorphicType<string>(
            "string",
            (writer, value, options) => { writer.WriteStringValue(value); },
            (ref Utf8JsonReader reader, JsonSerializerOptions options) => 
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.String, "Expected string value");
                return reader.GetString();
            }
        );

        serializer.RegisterPolymorphicType<bool>(
            "bool",
            (writer, value, options) => { writer.WriteBooleanValue(value); },
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False, "Expected boolean value");
                return reader.GetBoolean();
            }
        );

        serializer.RegisterPolymorphicType<byte[]>(
            "byteArray",
            (writer, value, options) =>
            {
                writer.WriteStartArray();
                foreach (var b in value)
                {
                    writer.WriteNumberValue(b);
                }
                writer.WriteEndArray();
            }, 
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                var byteList = new List<byte>();
                DebugJson.Assert(reader.TokenType == JsonTokenType.StartArray);
                
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    DebugJson.Assert(reader.TokenType == JsonTokenType.Number, "Expected number in byte array");
                    byteList.Add(reader.GetByte());
                }
                
                return byteList.ToArray();
            }
        );

        serializer.RegisterPolymorphicType<int[]>(
            "intArray",
            (writer, value, options) =>
            {
                writer.WriteStartArray();
                foreach (var i in value)
                {
                    writer.WriteNumberValue(i);
                }
                writer.WriteEndArray();
            }, 
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                var intList = new List<int>();
                
                DebugJson.Assert(reader.TokenType == JsonTokenType.StartArray);
                
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    DebugJson.Assert(reader.TokenType == JsonTokenType.Number, "Expected number in int array");

                    intList.Add(reader.GetInt32());
                }
                
                return intList.ToArray();
            }
        );
        
        serializer.RegisterPolymorphicType<Vector3>(
            "vector3", 
            (writer, value, options) =>
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(value.X);
                writer.WriteNumberValue(value.Y);
                writer.WriteNumberValue(value.Z);
                writer.WriteEndArray();
            }, 
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            {
                DebugJson.Assert(reader.TokenType == JsonTokenType.StartArray);
                
                if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                    throw new JsonException("Expected number for X component of Vector3");
                var x = reader.GetSingle();
                if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                    throw new JsonException("Expected number for Y component of Vector3");
                var y = reader.GetSingle();
                if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                    throw new JsonException("Expected number for Z component of Vector3");
                var z = reader.GetSingle();
                
                if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException("Expected end of array for Vector3");
                
                return new Vector3(x, y, z);
            }
        );

        serializer.RegisterPolymorphicType<PlayerId>("playerId");
        
        foreach (var type in ReflectionHelpers.GetTypesWithAttribute<RegisterJsonConverterAttribute>())
        {
            if (!typeof(JsonConverter).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type.FullName} is marked with [RegisterJsonConverter] but does not derive from JsonConverter");

            var inst = Activator.CreateInstance(type);
            if (inst == null)
                throw new InvalidOperationException($"Failed to instantiate a JSON converter {type.FullName}");
            
            serializer.RegisterConverter((JsonConverter)inst);
        }
    }
}