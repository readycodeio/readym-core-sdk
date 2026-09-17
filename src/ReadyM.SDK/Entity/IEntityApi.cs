using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Exceptions;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entity;

internal interface IEntityApi
{
    int ComponentIdOf(Type type);

    /// <summary>Where the component lives, or a default that is not <c>Found</c> if it is absent.</summary>
    /// <exception cref="InvalidEntityException">The entity is gone.</exception>
    ComponentRef Locate(RawEntity rawEntity, int componentId);

    void AddComponent<T>(RawEntity entity) where T : struct, IComponent;
    bool IsAlive(RawEntity rawEntity);

    /// <summary>Whether the entity carries every component of the set. An empty set matches anything.</summary>
    bool HasComponents(RawEntity rawEntity, ComponentSet components);
}
