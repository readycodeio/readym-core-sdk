using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal interface INetworkedComponentRegistryCallback : IComponentRegistryCallbackBase<INetworkedComponentRegistry, INetworkedComponent>
{
    // empty
}