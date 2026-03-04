using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

public interface IPlayerComponentRegistryCallback : IComponentRegistryCallbackBase<IPlayerComponentRegistry, IComponent>
{
    // empty
}