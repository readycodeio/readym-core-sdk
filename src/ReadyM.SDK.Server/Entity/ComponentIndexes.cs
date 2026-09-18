using Friflo.Engine.ECS;

namespace ReadyM.SDK.Server.Entity;

/// The indexes this runtime keeps for components the relay stores but cannot index itself.
internal sealed class ComponentIndexes
{
    private readonly Dictionary<Type, object> _tables = [];

    internal void Remember<TComponent, TKey>(TKey key, RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
        => Table<TComponent, TKey>()[key] = entity;

    /// Only drops the entry when it still names this entity, so a stale key cannot evict a live one.
    internal void Forget<TComponent, TKey>(TKey key, RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        var table = Table<TComponent, TKey>();

        if (table.TryGetValue(key, out var found) && found == entity)
            table.Remove(key);
    }

    internal bool TryFind<TComponent, TKey>(TKey key, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
        => Table<TComponent, TKey>().TryGetValue(key, out entity);

    private Dictionary<TKey, RawEntity> Table<TComponent, TKey>()
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        if (!_tables.TryGetValue(typeof(TComponent), out var table))
            _tables[typeof(TComponent)] = table = new Dictionary<TKey, RawEntity>();

        return (Dictionary<TKey, RawEntity>)table;
    }
}
