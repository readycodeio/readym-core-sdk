using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Entities;

/// <summary>Every entity on the server: create, query, look up and delete them.</summary>
public interface IEntities : IEntitiesBase
{
    /// <summary>Create a new entity of the given archetype, in a given scope, owned by the given player.</summary>
    T Create<T>(Scope scope, PlayerId owner) where T : struct, IArchetype;

    /// <summary>Iterate over every entity of a given archetype or containting a given mixin.</summary>
    /// <typeparam name="T">Archetype or Mixin</typeparam>
    EntityQuery<T> Query<T>() where T : struct, IArchetypeQueryable;

    /// <summary>
    /// The entity holding a key held once in the whole world (a player id), in whichever dimension it is. A key held
    /// once per dimension (an area, a cell) does not compile here: look it up with its dimension.
    /// </summary>
    /// <returns><c>false</c> when nothing holds that value.</returns>
    bool TryLookup<T, TKey>(TKey key, out T shape)
        where T : struct, IArchetypeQueryable, IIndexedOnce<TKey>;

    /// <summary>The entity holding the key in one dimension, or a shared one.</summary>
    /// <returns><c>false</c> when nothing there holds that value.</returns>
    bool TryLookup<T, TKey>(TKey key, DimensionId dimension, out T shape)
        where T : struct, IArchetypeQueryable, IIndexed<TKey>;

    /// <summary>Remove an entity from the world.</summary>
    /// <returns><c>false</c> if the entity was already deleted.</returns>
    bool Delete<T>(in T shape) where T : IEntityShape, allows ref struct;

    /// <summary>Iterate over every entity carrying both mixins.</summary>
    EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin;

    /// <summary>Iterate over every entity carrying all 3 mixins.</summary>
    EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin;

    /// <summary>Iterate over every entity carrying all 4 mixins.</summary>
    EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin;

    /// <summary>Iterate over every entity carrying all 5 mixins.</summary>
    EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin;

    /// <summary>Iterate over every entity carrying all 6 mixins.</summary>
    EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin;
}
