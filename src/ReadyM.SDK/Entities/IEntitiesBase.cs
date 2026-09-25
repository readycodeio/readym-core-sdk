using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Core;

namespace ReadyM.SDK.Entities;

public interface IEntitiesBase
{
    /// Create a new entity of a given Archetype.
    T Create<T>() where T : struct, IArchetype;
    
    World World { get; }
    
    /// The entity whose indexed value is the key, as the shape carrying the index.
    /// <returns><c>false</c> when nothing holds that value.</returns>
    bool TryLookup<T, TKey>(TKey key, out T shape) where T : struct, IArchetypeQueryable, IIndexed<TKey>;

    /// Creates an entity held by a scope, which removes it when the scope goes.
    T Create<T>(Scope scope) where T : struct, IArchetype;
}