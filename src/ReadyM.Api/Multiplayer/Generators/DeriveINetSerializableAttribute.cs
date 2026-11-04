using System;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Generators;

/// <summary>
/// Decorate a struct with this attribute to make it available for use as an RPC parameter.
/// </summary>
/// <param name="mode">Determines which members of the struct are serialized. See <see cref="SerializableMode"/> for details.</param>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class DeriveINetSerializableAttribute(SerializableMode mode = SerializableMode.Default) : Attribute
{
    public readonly SerializableMode Mode = mode;
}