using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal class DefaultAreaArchetypeRegistration(IAreaComponentRegistry areaComponentRegistry) : IArchetypeRegistration
{
    private class RegisterAreaComponentsCallback(EntityBuilder builder) : IAreaComponentRegistryCallback
    {
        public void AcceptComponent<T>(IAreaComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add(defaultValue);
        }
    }
    
    public ArchetypeId AreaArchetype { get; private set; }

    public void Register(Store world)
    {
        AreaArchetype = world.RegisterArchetype(
            b =>
            {
                b.Add<MetadataComponent>();
                b.Add<AreaScopeComponent>();
                b.AddTag<ScopeEntityTag>();
                areaComponentRegistry.Accept(new RegisterAreaComponentsCallback(b));
            }
        );
    }
}