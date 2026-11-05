using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class DefaultNativeComponentRegistration : INativeComponentRegistration
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