using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Managers;

/// <exclude />
public interface IEntityDeleteImpl
{
    void HandleDelete(Entity entity);
}