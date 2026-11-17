using Friflo.Engine.ECS;

namespace ReadyM.SDK.Server.Entities;

/// <summary>
/// The indexes this runtime keeps for components the relay stores but cannot index itself. A key can have a holder in
/// each dimension, so each key keeps every holder written through this runtime, oldest first.
/// </summary>
internal sealed class ComponentIndexes
{
    private readonly Dictionary<Type, object> _tables = [];

    internal void Remember<TComponent, TKey>(TKey key, RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        var table = Table<TComponent, TKey>();

        if (!table.TryGetValue(key, out var holders))
            table[key] = holders = [];

        if (!holders.Contains(entity))
            holders.Add(entity);
    }

    /// <summary>Drops only this entity, so a stale key cannot evict a live holder.</summary>
    internal void Forget<TComponent, TKey>(TKey key, RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        var table = Table<TComponent, TKey>();

        if (table.TryGetValue(key, out var holders) && holders.Remove(entity) && holders.Count == 0)
            table.Remove(key);
    }

    /// <summary>The holders remembered for the key; empty when none. Live: forgetting one changes it.</summary>
    internal IReadOnlyList<RawEntity> Holders<TComponent, TKey>(TKey key)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
        => Table<TComponent, TKey>().TryGetValue(key, out var holders) ? holders : [];

    private Dictionary<TKey, List<RawEntity>> Table<TComponent, TKey>()
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        if (!_tables.TryGetValue(typeof(TComponent), out var table))
            _tables[typeof(TComponent)] = table = new Dictionary<TKey, List<RawEntity>>();

        return (Dictionary<TKey, List<RawEntity>>)table;
    }
}
