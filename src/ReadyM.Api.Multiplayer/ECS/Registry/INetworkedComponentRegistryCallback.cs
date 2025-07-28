using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

public interface INetworkedComponentRegistryCallback
{
    void AcceptNetworkedComponent<T>()
        where T : struct, INetworkedComponent;
}