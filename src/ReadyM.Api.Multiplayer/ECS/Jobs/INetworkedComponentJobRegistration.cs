using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal interface INetworkedComponentJobRegistration
{
    void Register(INetworkedComponentRegistry registry, NetworkedComponentJobRegistry jobRegistry);
}