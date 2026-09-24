using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Access;

namespace ReadyM.SDK.Client.Mapping;

public interface IShapeMappingScope<TShape, out TContext>
    where TShape : struct, IArchetypeQueryable
{
    /// Declare a mapping between a single shape field and a single context object. Leaving out the
    /// push says the game owns the value and will not be told: a shape declaring
    /// <c>Propagation.ToEcsOnly</c> has nothing to push.
    IShapeMappingScope<TShape, TContext> Map<TValue>(
        Value<TShape, TValue> value,
        Pull<TValue, TContext> pull,
        Action<TValue, TContext>? push = null);

    /// Declare a mapping for one collection. The handler is handed the collection as its component
    /// exposes it rather than the collection itself, so what it changes is what the entity holds.
    IShapeMappingScope<TShape, TContext> Map<TAccess>(
        Collection<TShape, TAccess> collection,
        Collected<TAccess, TContext> pull,
        Collected<TAccess, TContext>? push = null)
#if NET10_0_OR_GREATER
        where TAccess : allows ref struct
#endif
        ;

    /// Declare a mapping between the entire shape and a single context object.
    /// Used for mappings that set multiple fields at once.
    IShapeMappingScope<TShape, TContext> Map(
        Shape<TShape> shape,
        Action<TShape, TContext> pull,
        Action<TShape, TContext>? push = null);
}
