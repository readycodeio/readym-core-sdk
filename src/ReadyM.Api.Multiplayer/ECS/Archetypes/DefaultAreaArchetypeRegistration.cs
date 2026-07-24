using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultAreaArchetypeRegistration(IAreaComponentRegistry areaComponentRegistry) : IArchetypeRegistration
{
    private class RegisterAreaComponentsCallback(EntityBuilderBase builder) : IAreaComponentRegistryCallback
    {
        public void AcceptComponent<T>(IAreaComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add(defaultValue);
        }
    }
    
    public ArchetypeId AreaArchetype { get; private set; }

    public void Register(IArchetypeRegistry registry)
    {
        AreaArchetype = registry.RegisterArchetype(
            b =>
            {
                b.Add<MetadataComponent>();
                b.Add<AreaScopeComponent>();
                b.Add<EmptyScopeDeletionComponent>();
                b.AddTag<ScopeEntityTag>();
                areaComponentRegistry.Accept(new RegisterAreaComponentsCallback(b));
            }
        );
    }
}