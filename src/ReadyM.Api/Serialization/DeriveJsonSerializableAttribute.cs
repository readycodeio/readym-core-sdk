using System;

namespace ReadyM.Api.Serialization;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class DeriveJsonSerializableAttribute(SerializableMode mode = SerializableMode.Default) : Attribute
{
    public readonly SerializableMode Mode = mode;
}