using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Mapping;

internal interface IMappedEntityManager<TGameObject>
{
    // Is the mapping system aware of the entity / game object. If not, the game object should not be managed
    // i.e. the game code should function as in the vanilla base game. 
    bool IsMapped(Entity entity);
    bool IsMapped(Entity entity, [NotNullWhen(true)] out TGameObject? gameObj);
    bool IsMapped(TGameObject? gameObj, [NotNullWhen(true)] out Entity? entity);
    
    void AddMappedEntity(Entity entity, TGameObject gameObj);
    void RemoveMappedEntity(Entity entity);
    void RemoveMappedEntity(TGameObject gameObj);
}
