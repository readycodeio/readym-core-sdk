using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

public interface INetworkedComponentRegistry
{
    INetworkedComponentRegistry RegisterComponent<T>() where T : struct, INetworkedComponent;
    
    // NOTE: Visitor pattern to handle generics without reflection.
    void Accept(INetworkedComponentRegistryCallback callback);
}