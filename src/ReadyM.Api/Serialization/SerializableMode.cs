using System;

namespace ReadyM.Api.Serialization;

[Flags]
public enum SerializableMode : byte
{
    MapFields = 1 << 0,
    MapProperties = 1 << 1,
    MapFieldsAndProperties = MapFields | MapProperties,
    MapPrivate = 1 << 2,
    MapPublic = 1 << 3,
    MapInternal = 1 << 4,
    Default = MapFields | MapPrivate
}