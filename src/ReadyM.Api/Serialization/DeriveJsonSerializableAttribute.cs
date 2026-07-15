using System;

namespace ReadyM.Api.Serialization;

/// <exclude />
[AttributeUsage(AttributeTargets.Struct)]
public sealed class DeriveJsonSerializableAttribute(SerializableMode mode = SerializableMode.Default) : Attribute
{
    public readonly SerializableMode Mode = mode;
}