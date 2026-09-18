namespace ReadyM.SDK.Archetypes;

/// Marks an archetype whose entities can hold others: entities created in it are removed with it,
/// and a query can be narrowed to it. Declare it on the shape itself, next to [Archetype].
public interface IScope;
