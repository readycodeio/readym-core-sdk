using System;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Generators;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class DeriveINetSerializableAttribute(SerializableMode mode = SerializableMode.Default) : Attribute
{
    public readonly SerializableMode Mode = mode;
}