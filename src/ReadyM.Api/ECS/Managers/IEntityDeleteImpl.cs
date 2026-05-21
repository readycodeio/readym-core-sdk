using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Managers;

public interface IEntityDeleteImpl
{
    void HandleDelete(Entity entity);
}