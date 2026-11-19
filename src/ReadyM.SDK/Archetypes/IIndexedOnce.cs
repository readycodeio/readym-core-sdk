namespace ReadyM.SDK.Archetypes;

/// <summary>
/// Marks a shape indexed by a key held once in the whole world, so it can be looked up with no dimension. A key held
/// once per dimension (an area, a cell) has only <see cref="IIndexed{TKey}"/> and is looked up with its dimension.
/// </summary>
public interface IIndexedOnce<TKey> : IIndexed<TKey>;
