using System.ComponentModel;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.SDK.Archetypes.Access;

/// Names one of a shape's collections, the way <see cref="Value{TShape,TValue}"/> names one of its values.
public readonly struct Collection<TShape, TAccess>
    where TShape : struct, IArchetypeQueryable
#if NET10_0_OR_GREATER
    where TAccess : allows ref struct
#endif
{
    internal ValueAccess Value { get; }

    internal CollectionAccess<TAccess> Open { get; }

    internal Collection(ValueAccess value, CollectionAccess<TAccess> open)
    {
        Value = value;
        Open = open;
    }
}

/// Called by generated code.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Collection
{
    public static Collection<TShape, TAccess> Of<TShape, TComponent, TValue, TAccess>(
        Field<TComponent, TValue> field,
        CollectionAccess<TAccess> open)
        where TShape : struct, IArchetypeQueryable
        where TComponent : struct, IReadyComponent
#if NET10_0_OR_GREATER
        where TAccess : allows ref struct
#endif
        => new(new ValueAccess<TComponent, TValue>(field), open);
}
