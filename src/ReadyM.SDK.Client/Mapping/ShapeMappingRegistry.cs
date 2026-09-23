using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Access;

namespace ReadyM.SDK.Client.Mapping;

internal sealed class ShapeMappingRegistry : IShapeMappingRegistry
{
    private readonly Dictionary<(Type Component, Type Context, int Field), object> _mappings = [];

    public IShapeMappingScope<TShape, TContext> For<TShape, TContext>()
        where TShape : struct, IArchetypeQueryable
        => new Scope<TShape, TContext>(this);

    internal Mapping<TValue, TContext>? Find<TValue, TContext>(ValueAccess<TValue> access)
        => _mappings.TryGetValue((access.Component, typeof(TContext), access.Field), out var found)
            ? (Mapping<TValue, TContext>)found
            : null;

    private void Add<TValue, TContext>(ValueAccess<TValue> access, Mapping<TValue, TContext> mapping)
        => _mappings[(access.Component, typeof(TContext), access.Field)] = mapping;

    /// A shape-wide mapping sits under the same key with no field, which nothing else can take.
    private const int WholeShape = -1;

    internal Mapping<TShape, TContext>? FindShape<TShape, TContext>(ShapeAccess access)
        => _mappings.TryGetValue((access.Component, typeof(TContext), WholeShape), out var found)
            ? (Mapping<TShape, TContext>)found
            : null;

    private void Add<TShape, TContext>(ShapeAccess access, Mapping<TShape, TContext> mapping)
        => _mappings[(access.Component, typeof(TContext), WholeShape)] = mapping;

    internal sealed class Mapping<TValue, TContext>(Action<TValue, TContext> push, Pull<TValue, TContext> pull)
    {
        public Action<TValue, TContext> Push { get; } = push;

        public Pull<TValue, TContext> Pull { get; } = pull;
    }

    private sealed class Scope<TShape, TContext>(ShapeMappingRegistry registry) : IShapeMappingScope<TShape, TContext>
        where TShape : struct, IArchetypeQueryable
    {
        public IShapeMappingScope<TShape, TContext> Map<TValue>(
            Value<TShape, TValue> value,
            Action<TValue, TContext> push,
            Pull<TValue, TContext> pull)
        {
            registry.Add(value.Access, new Mapping<TValue, TContext>(push, pull));
            return this;
        }

        public IShapeMappingScope<TShape, TContext> Map(
            Shape<TShape> shape,
            Action<TShape, TContext> push,
            Action<TShape, TContext> pull)
        {
            registry.Add(shape.Access, new Mapping<TShape, TContext>(push, (ref s, ctx) => pull(s, ctx)));
            return this;
        }
    }
}
