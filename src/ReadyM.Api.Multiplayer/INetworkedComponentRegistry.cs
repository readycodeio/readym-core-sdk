namespace ReadyM.Api.Multiplayer;

public interface INetworkedComponentRegistry
{
    INetworkedComponentRegistry RegisterComponent<T>() where T : struct, INetworkedComponent;
    
    // NOTE: Visitor pattern to handle generics without reflection.
    void Accept(INetworkedComponentRegistryCallback callback);
}