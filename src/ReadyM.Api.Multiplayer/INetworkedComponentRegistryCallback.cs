namespace ReadyM.Api.Multiplayer;

public interface INetworkedComponentRegistryCallback
{
    void AcceptNetworkedComponent<T>()
        where T : struct, INetworkedComponent;
}