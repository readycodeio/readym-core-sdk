using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.ECS.Components;

namespace ReadyM.Relay.Common.ECS.Registry;

public class DefaultNativeComponentRegistration : INativeComponentRegistration
{
    public void Register(INativeComponentRegistry registry)
    {
        registry.RegisterComponent<LocallyCreatedEntityTag>();
        registry.RegisterComponent<MetadataComponent>();
        registry.RegisterComponent<ScopeEntityTag>();
        registry.RegisterComponent<AreaScopeComponent>();
        registry.RegisterComponent<PlayerScopeComponent>();
    }
}