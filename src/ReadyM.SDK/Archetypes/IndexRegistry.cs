using System.Collections.Concurrent;
using System.ComponentModel;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes;

/// <summary>Which component holds the index behind each shape, so a lookup can reach it by the shape alone.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IndexRegistry
{
    private static readonly ConcurrentDictionary<Type, object> Finders = new();

    /// <summary>Called by generated code for an indexed shape.</summary>
    public static void Register<TShape, TKey, TComponent>()
        where TShape : struct, IArchetypeQueryable, IIndexed<TKey>
        where TComponent : struct, IIndexedComponent<TKey>
        where TKey : notnull
        => Finders[typeof(TShape)] = new Finder<TKey, TComponent>();

    internal static bool TryFind<TShape, TKey>(IEntityApi api, TKey key, out RawEntity entity)
    {
        if (Finders.TryGetValue(typeof(TShape), out var finder) && finder is Finder<TKey> typed)
            return typed.TryFind(api, key, out entity);

        entity = default;
        return false;
    }

    /// <summary>
    /// A lookup in the dimensions <paramref name="filter"/> sees, where the api keeps dimensions; a unique one is refused
    /// across two.
    /// </summary>
    internal static bool TryFind<TShape, TKey>(IEntityApi api, TKey key, DimensionFilter filter, bool unique, out RawEntity entity)
    {
        if (Finders.TryGetValue(typeof(TShape), out var finder) && finder is Finder<TKey> typed)
            return api is IDimensionLookup dimensions
                ? typed.TryFindIn(dimensions, key, filter, unique, out entity)
                : typed.TryFind(api, key, out entity);

        entity = default;
        return false;
    }

    // The component type is only known where the shape was generated, so it is captured here rather
    // than travelling with the call. The api never has to name it.
    private abstract class Finder<TKey>
    {
        internal abstract bool TryFind(IEntityApi api, TKey key, out RawEntity entity);

        internal abstract bool TryFindIn(IDimensionLookup api, TKey key, DimensionFilter filter, bool unique, out RawEntity entity);
    }

    private sealed class Finder<TKey, TComponent> : Finder<TKey>
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        internal override bool TryFind(IEntityApi api, TKey key, out RawEntity entity)
            => api.TryFindByIndex<TComponent, TKey>(key, out entity);

        internal override bool TryFindIn(IDimensionLookup api, TKey key, DimensionFilter filter, bool unique, out RawEntity entity)
            => api.TryFindByIndex<TComponent, TKey>(key, filter, unique, out entity);
    }
}
