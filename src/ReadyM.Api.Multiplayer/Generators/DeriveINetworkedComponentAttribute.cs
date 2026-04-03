using System;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Generators;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class DeriveINetworkedComponentAttribute(SerializableMode mode = SerializableMode.Default, bool emitDirtyMask = true) : Attribute
{
    public readonly SerializableMode Mode = mode;
    public readonly bool EmitDirtyMask = emitDirtyMask;
}