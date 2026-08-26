using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultAreaArchetypeRegistration(IAreaComponentRegistry areaComponentRegistry) : IArchetypeRegistration
{
    private class RegisterAreaComponentsCallback(ArchetypeBuilder builder) : IAreaComponentRegistryCallback
    {
        public void AcceptComponent<T>(IAreaComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add<T>();
        }
    }

    public ArchetypeId AreaArchetype { get; private set; }

    public void Register(IArchetypeRegistry registry)
    {
        AreaArchetype = registry.RegisterArchetype(
            new ArchetypeBuilder()
                .Add<MetadataComponent>()
                .Add<AreaScopeComponent>()
                .Add<EmptyScopeDeletionComponent>()
                .AddTag<ScopeEntityTag>()
                .With(b => areaComponentRegistry.Accept(new RegisterAreaComponentsCallback(b)))
        );
    }
}
