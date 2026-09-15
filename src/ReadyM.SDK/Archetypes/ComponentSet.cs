namespace ReadyM.SDK.Archetypes;

/// <summary>
/// Represents a set of component types that make up an archetype or mixin.
/// A general class like this is required since the client uses ComponentTypes and the server uses numeric IDs.
/// </summary>
public sealed class ComponentSet
{
    public static readonly ComponentSet Empty = new([]);

    private ComponentSet(Type[] types) => Types = types;

    public Type[] Types { get; }

    public static ComponentSet Of<T1>()
        where T1 : struct
        => Cache<T1>.Set;

    public static ComponentSet Of<T1, T2>()
        where T1 : struct where T2 : struct
        => Cache<T1, T2>.Set;

    public static ComponentSet Of<T1, T2, T3>()
        where T1 : struct where T2 : struct where T3 : struct
        => Cache<T1, T2, T3>.Set;

    public static ComponentSet Of<T1, T2, T3, T4>()
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        => Cache<T1, T2, T3, T4>.Set;

    public static ComponentSet Of<T1, T2, T3, T4, T5>()
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
        => Cache<T1, T2, T3, T4, T5>.Set;

    public static ComponentSet Of<T1, T2, T3, T4, T5, T6>()
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
        => Cache<T1, T2, T3, T4, T5, T6>.Set;

    public override string ToString() => string.Join(", ", Types.Select(type => type.Name));

    private static class Cache<T1>
    {
        internal static readonly ComponentSet Set = new([typeof(T1)]);
    }

    private static class Cache<T1, T2>
    {
        internal static readonly ComponentSet Set = new([typeof(T1), typeof(T2)]);
    }

    private static class Cache<T1, T2, T3>
    {
        internal static readonly ComponentSet Set = new([typeof(T1), typeof(T2), typeof(T3)]);
    }

    private static class Cache<T1, T2, T3, T4>
    {
        internal static readonly ComponentSet Set = new([typeof(T1), typeof(T2), typeof(T3), typeof(T4)]);
    }

    private static class Cache<T1, T2, T3, T4, T5>
    {
        internal static readonly ComponentSet Set = new([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)]);
    }

    private static class Cache<T1, T2, T3, T4, T5, T6>
    {
        internal static readonly ComponentSet Set = new([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)]);
    }
}
