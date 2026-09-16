using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entity;

internal interface IEntityApi
{
    public bool HasComponent<T>(RawEntity rawEntity) where T : struct, IComponent;
    public ref T GetComponent<T>(RawEntity rawEntity) where T : struct, IComponent;
    bool TryGetComponent<T>(RawEntity rawEntity, out T component) where T : struct, IComponent;
    void AddComponent<T>(RawEntity entity) where T : struct, IComponent;
    bool IsAlive(RawEntity rawEntity);

    /// <summary>Whether the entity carries every component of the set. An empty set matches anything.</summary>
    bool HasComponents(RawEntity rawEntity, ComponentSet components);
}