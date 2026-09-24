using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Access;

namespace ReadyM.SDK.Client.Mapping;

internal sealed class ShapeMappingRegistry : IShapeMappingRegistry
{
    private readonly Dictionary<(Type Component, Type Context, int Field), object> _mappings = [];

    private readonly Dictionary<(Type Component, Type Context, int Field, Way Way), object> _collections = [];

    public IShapeMappingScope<TShape, TContext> For<TShape, TContext>()
        where TShape : struct, IArchetypeQueryable
        => new Scope<TShape, TContext>(this);

    internal Collected<TAccess, TContext>? FindPull<TAccess, TContext>(ValueAccess access)
#if NET10_0_OR_GREATER
        where TAccess : allows ref struct
#endif
        => Collected<TAccess, TContext>(access, Way.Pull);

    internal Collected<TAccess, TContext>? FindPush<TAccess, TContext>(ValueAccess access)
#if NET10_0_OR_GREATER
        where TAccess : allows ref struct
#endif
        => Collected<TAccess, TContext>(access, Way.Push);

    /// A handler over a collection cannot be held beside the others: on a runtime carrying ref
    /// structs it is a delegate whose parameter is one, so the pair is kept apart instead.
    private Collected<TAccess, TContext>? Collected<TAccess, TContext>(ValueAccess access, Way way)
#if NET10_0_OR_GREATER
        where TAccess : allows ref struct
#endif
        => _collections.TryGetValue((access.Component, typeof(TContext), access.Field, way), out var found)
            ? (Collected<TAccess, TContext>)found
            : null;

    private void Add<TAccess, TContext>(
        ValueAccess access,
        Collected<TAccess, TContext> pull,
        Collected<TAccess, TContext>? push)
#if NET10_0_OR_GREATER
        where TAccess : allows ref struct
#endif
    {
        _collections[(access.Component, typeof(TContext), access.Field, Way.Pull)] = pull;

        if (push is not null)
            _collections[(access.Component, typeof(TContext), access.Field, Way.Push)] = push;
    }

    private enum Way
    {
        Pull,
        Push
    }

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

    internal sealed class Mapping<TValue, TContext>(Action<TValue, TContext>? push, Pull<TValue, TContext> pull)
    {
        /// Null for a value the game owns and will not be told.
        public Action<TValue, TContext>? Push { get; } = push;

        public Pull<TValue, TContext> Pull { get; } = pull;
    }

    private sealed class Scope<TShape, TContext>(ShapeMappingRegistry registry) : IShapeMappingScope<TShape, TContext>
        where TShape : struct, IArchetypeQueryable
    {
        public IShapeMappingScope<TShape, TContext> Map<TValue>(
            Value<TShape, TValue> value,
            Pull<TValue, TContext> pull,
            Action<TValue, TContext>? push = null)
        {
            registry.Add(value.Access, new Mapping<TValue, TContext>(push, pull));
            return this;
        }

        public IShapeMappingScope<TShape, TContext> Map<TAccess>(
            Collection<TShape, TAccess> collection,
            Collected<TAccess, TContext> pull,
            Collected<TAccess, TContext>? push = null)
#if NET10_0_OR_GREATER
            where TAccess : allows ref struct
#endif
        {
            registry.Add(collection.Value, pull, push);
            return this;
        }

        public IShapeMappingScope<TShape, TContext> Map(
            Shape<TShape> shape,
            Action<TShape, TContext> pull,
            Action<TShape, TContext>? push = null)
        {
            registry.Add(
                shape.Access,
                new Mapping<TShape, TContext>(push, (ref TShape s, TContext ctx) => pull(s, ctx)));

            return this;
        }
    }
}
