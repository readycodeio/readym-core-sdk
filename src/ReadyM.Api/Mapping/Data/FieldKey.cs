using System;

namespace ReadyM.Api.Mapping.Data;

internal readonly record struct FieldKey(Type ComponentType, Type ContextType, int FieldId)
{
    internal Type ComponentType { get; } = ComponentType;
    internal Type ContextType { get; } = ContextType;
    internal int FieldId { get; } = FieldId;
}