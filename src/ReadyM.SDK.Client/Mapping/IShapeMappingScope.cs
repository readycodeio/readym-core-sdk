using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Access;

namespace ReadyM.SDK.Client.Mapping;

public interface IShapeMappingScope<TShape, out TContext>
    where TShape : struct, IArchetypeQueryable
{
    /// Declare a mapping between a single shape field and a single context object.
    IShapeMappingScope<TShape, TContext> Map<TValue>(
        Value<TShape, TValue> value,
        Action<TValue, TContext> push,
        Pull<TValue, TContext> pull);

    /// Declare a mapping between a the entire shape and a single context object.
    /// Used for mappings that set multiple fields at once.
    IShapeMappingScope<TShape, TContext> Map(
        Shape<TShape> shape,
        Action<TShape, TContext> push,
        Action<TShape, TContext> pull);
}