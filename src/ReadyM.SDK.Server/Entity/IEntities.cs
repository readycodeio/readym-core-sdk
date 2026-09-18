using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Server.Entity;

public interface IEntities
{
    /// Iterate over every entity of a given archetype or containting a given mixin.
    /// <typeparam name="T">Archetype or Mixin</typeparam>
    EntityQuery<T> Query<T>() where T : struct, IArchetypeQueryable;

    /// Create a new entity of a given archetype. 
    /// <typeparam name="T">Archetype</typeparam>
    T Create<T>() where T : struct, IArchetype;

    /// Remove an entity from the world.
    /// <returns><c>false</c> if the entity was already deleted.</returns>
    bool Delete<T>(in T shape) where T : IEntityShape, allows ref struct;

    /// Iterate over every entity carrying both mixins.
    EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin;

    /// Iterate over every entity carrying all 3 mixins.
    EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin;

    /// Iterate over every entity carrying all 4 mixins.
    EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin;

    /// Iterate over every entity carrying all 5 mixins.
    EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin;

    /// Iterate over every entity carrying all 6 mixins.
    EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin;

    /// The entity whose indexed value is the key, as the shape carrying the index.
    /// <returns><c>false</c> when nothing holds that value.</returns>
    bool TryLookup<T, TKey>(TKey key, out T shape) where T : struct, IArchetypeQueryable, IIndexed<TKey>;

    /// Creates an entity held by a scope, which removes it when the scope goes.
    T Create<T>(Scope scope) where T : struct, IArchetype;
}
