using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultPlayerArchetypeRegistration(IPlayerComponentRegistry playerComponentRegistry) : IArchetypeRegistration
{
    private class RegisterPlayerComponentsCallback(EntityBuilderBase builder) : IPlayerComponentRegistryCallback
    {
        public void AcceptComponent<T>(IPlayerComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add(defaultValue);
        }
    }
    
    public ArchetypeId PlayerArchetype { get; private set; }

    public void Register(IArchetypeRegistry registry)
    {
        PlayerArchetype = registry.RegisterArchetype(
            b =>
            {
                b.Add<MetadataComponent>();
                b.Add<PlayerScopeComponent>();
                b.AddTag<ScopeEntityTag>();
                playerComponentRegistry.Accept(new RegisterPlayerComponentsCallback(b));
            }
        );
    }
}