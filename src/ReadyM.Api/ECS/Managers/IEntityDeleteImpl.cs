using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Managers;

internal interface IEntityDeleteImpl
{
    void HandleDelete(Entity entity);
}