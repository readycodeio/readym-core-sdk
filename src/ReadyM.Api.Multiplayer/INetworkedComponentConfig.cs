namespace ReadyM.Api.Multiplayer;

public interface INetworkedComponentConfig
{
    INetworkedComponentConfig SynchronizeComponent<T>() where T : struct, INetworkedComponent;
}