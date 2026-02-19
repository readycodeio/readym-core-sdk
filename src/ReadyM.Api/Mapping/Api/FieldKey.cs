using System;

namespace ReadyM.Api.Mapping.Api;

internal readonly record struct FieldKey(Type ComponentType, int FieldId)
{
    public Type ComponentType { get; } = ComponentType;
    public int FieldId { get; } = FieldId;
}