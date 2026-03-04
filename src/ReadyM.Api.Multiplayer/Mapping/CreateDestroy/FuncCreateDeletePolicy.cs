using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.CreateDestroy;

public class FuncCreateDeletePolicy<TGameObject>(
    Func<TGameObject, bool> shouldCreatePropagate,
    Func<Entity, bool> shouldDeletePropagate) : IMappingCreateDeletePolicy<TGameObject>
    where TGameObject : class
{
    public Type GameObjectType
        => typeof(TGameObject);
    
    public bool ShouldGameCreatePropagateToEcs(TGameObject gameObj)
        => shouldCreatePropagate(gameObj);

    public bool ShouldGameDeletePropagateToEcs(Entity entity)
        => shouldDeletePropagate(entity);
}