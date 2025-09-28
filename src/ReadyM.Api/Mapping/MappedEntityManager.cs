using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.ECS.Worlds;

namespace ReadyM.Api.Mapping;

public class MappedEntityManager<TGameObject>(Store world) : IMappedEntityManager<TGameObject>
    where TGameObject : class
{
    private readonly ComponentIndex<MappingComponent<TGameObject>, TGameObject> _ix = world.ComponentIndex<MappingComponent<TGameObject>, TGameObject>();

    public bool IsMapped(Entity entity)
        => entity.HasComponent<MappingComponent<TGameObject>>();

    public bool IsMapped(Entity entity, [NotNullWhen(true)] out TGameObject? gameObj)
    {
        if (!entity.TryGetComponent<MappingComponent<TGameObject>>(out var mappingComp))
        {
            gameObj = null;
            return false;
        }
        
        gameObj = mappingComp.GameObject;
        return true;
    }
    
    public bool IsMapped(TGameObject? gameObj, [NotNullWhen(true)] out Entity? entity) 
    {
        if (gameObj == null)
        {
            entity = null;
            return false;
        }
        
        var matching = _ix[gameObj];
        switch (matching.Count)
        {
            case 0:
                entity = null;
                return false;
            case 1:
                entity = matching[0];
                return true;
            default:
                throw new System.InvalidOperationException(
                    $"Multiple entities mapped to the same game object {gameObj}.");
        }
    }

    public void AddMappedEntity(Entity entity, TGameObject gameObj)
        => entity.AddComponent(new MappingComponent<TGameObject>(gameObj));

    public void AddMappedEntity(CommandBuffer buffer, Entity entity, TGameObject gameObj)
        => buffer.AddComponent(entity.Id, new MappingComponent<TGameObject>(gameObj));

    public void SetMappedEntity(Entity entity, TGameObject gameObj)
        => entity.Set(new MappingComponent<TGameObject>(gameObj));
    
    public void RemoveMappedEntity(Entity entity)
    {
        if (!entity.RemoveComponent<MappingComponent<TGameObject>>())
            throw new KeyNotFoundException($"Entity {entity} not found in mapped entities.");
    }

    public void RemoveMappedEntity(TGameObject gameObj)
    {
        var matching = _ix[gameObj];
        switch (matching.Count)
        {
            case 0:
                throw new System.InvalidOperationException($"No entity mapped to the game object {gameObj}.");
            case 1:
                var entity = matching[0];
                entity.RemoveComponent<MappingComponent<TGameObject>>();
                break;
            default:
                throw new System.InvalidOperationException($"Multiple entities mapped to the same game object {gameObj}.");
        }
    }
    
    public void RemoveMappedEntity(CommandBuffer buffer, Entity entity)
    {
        if (!entity.HasComponent<MappingComponent<TGameObject>>())
            throw new KeyNotFoundException($"Entity {entity} not found in mapped entities.");

        buffer.RemoveComponent<MappingComponent<TGameObject>>(entity.Id);
    }
    
    public void RemoveMappedEntity(CommandBuffer buffer, TGameObject gameObj)
    {
        var matching = _ix[gameObj];
        switch (matching.Count)
        {
            case 0:
                throw new System.InvalidOperationException($"No entity mapped to the game object {gameObj}.");
            case 1:
                var entity = matching[0];
                buffer.RemoveComponent<MappingComponent<TGameObject>>(entity.Id);
                break;
            default:
                throw new System.InvalidOperationException($"Multiple entities mapped to the same game object {gameObj}.");
        }
    }
}