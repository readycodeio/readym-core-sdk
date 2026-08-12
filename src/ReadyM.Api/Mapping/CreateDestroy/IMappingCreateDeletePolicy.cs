using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping.CreateDestroy;

internal interface IMappingCreateDeletePolicy<in TGameObject> : IMappingCreateDeletePolicyBase
    where TGameObject : class
{
    // Should newly-created game objects be mapped to newly created entities 
    bool ShouldGameCreatePropagateToEcs(TGameObject gameObj);
    
    // Should a deleted game object be unmapped from entities
    bool ShouldGameDeletePropagateToEcs(Entity entity);
}