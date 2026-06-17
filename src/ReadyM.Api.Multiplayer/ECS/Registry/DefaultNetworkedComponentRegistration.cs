using LiteNetLib;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class DefaultNetworkedComponentRegistration : INetworkedComponentRegistration
{
    public void Register(INetworkedComponentRegistry registry)
    {
        registry.RegisterComponent<PlayerScopeComponent>(DeliveryMethod.ReliableOrdered);
        registry.RegisterComponent<AreaScopeComponent>(DeliveryMethod.ReliableOrdered);
    }
}