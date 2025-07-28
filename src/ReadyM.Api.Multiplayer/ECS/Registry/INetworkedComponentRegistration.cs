namespace ReadyM.Api.Multiplayer.ECS.Registry;

public interface INetworkedComponentRegistration
{
    void Register(INetworkedComponentRegistry registry);
}