using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;

namespace ReadyM.SDK.Entities;

/// <summary>Creating and finding entities, the same on the client and the server.</summary>
public interface IEntitiesBase
{
    /// <summary>Create a new entity of a given Archetype.</summary>
    T Create<T>() where T : struct, IArchetype;

    /// <summary>The entity that stands for the whole session; throws until it exists.</summary>
    World World { get; }

    /// <summary>The entity whose indexed value is the key, as the shape carrying the index.</summary>
    /// <returns><c>false</c> when nothing holds that value.</returns>
    bool TryLookup<T, TKey>(TKey key, out T shape)
        where T : struct, IArchetypeQueryable, IIndexed<TKey>;

    /// <summary>Creates an entity held by a scope, which removes it when the scope goes.</summary>
    T Create<T>(Scope scope) where T : struct, IArchetype;

    /// <summary>The scope of a given shape holding this entity, if one does.</summary>
    /// <returns><c>false</c> when the entity is not in a scope of this type, or at all.</returns>
    bool TryGetScope<TShape, TScope>(in TShape shape, out TScope scope)
#if NET
        where TShape : struct, IEntityShape, allows ref struct
#else
        where TShape : struct, IEntityShape
#endif
        where TScope : struct, IArchetypeQueryable, IScope;

    /// <inheritdoc cref="TryGetScope{TShape, TScope}(in TShape, out TScope)"/>
    public bool TryGetScope<TScope>(in EntityHandle handle, out TScope scope)
        where TScope : struct, IArchetypeQueryable, IScope;
}