using System.Collections.Concurrent;
using System.ComponentModel;
using Friflo.Engine.ECS;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes;

/// Which component holds the index behind each shape, so a lookup can reach it by the shape alone.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IndexRegistry
{
    private static readonly ConcurrentDictionary<Type, object> Finders = new();

    /// Called by generated code for an indexed shape.
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

    // The component type is only known where the shape was generated, so it is captured here rather
    // than travelling with the call. The api never has to name it.
    private abstract class Finder<TKey>
    {
        internal abstract bool TryFind(IEntityApi api, TKey key, out RawEntity entity);
    }

    private sealed class Finder<TKey, TComponent> : Finder<TKey>
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        internal override bool TryFind(IEntityApi api, TKey key, out RawEntity entity)
            => api.TryFindByIndex<TComponent, TKey>(key, out entity);
    }
}
