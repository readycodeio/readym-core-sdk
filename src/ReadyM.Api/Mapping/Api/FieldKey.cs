using System;

namespace ReadyM.Api.Mapping.Api;

internal readonly record struct FieldKey(Type ComponentType, int FieldId)
{
    internal Type ComponentType { get; } = ComponentType;
    internal int FieldId { get; } = FieldId;
}