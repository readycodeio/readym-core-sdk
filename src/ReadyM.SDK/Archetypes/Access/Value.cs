using System.ComponentModel;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.SDK.Archetypes.Access;

/// Strongly typed helper for a field on an archetype.
public readonly struct Value<TShape, TValue>
    where TShape : struct, IArchetypeQueryable
{
    internal ValueAccess<TValue> Access { get; }

    internal Value(ValueAccess<TValue> access) => Access = access;
}

/// Called by generated code.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Value
{
    public static Value<TShape, TValue> Of<TShape, TComponent, TValue>(Field<TComponent, TValue> field)
        where TShape : struct, IArchetypeQueryable
        where TComponent : struct, IReadyComponent
        => new(new ValueAccess<TComponent, TValue>(field));
}