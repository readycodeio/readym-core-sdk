using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultWorldArchetypeRegistration(IWorldComponentRegistry worldComponentRegistry) : IArchetypeRegistration
{
    private class RegisterWorldComponentsCallback(EntityBuilderBase builder) : IWorldComponentRegistryCallback
    {
        public void AcceptComponent<T>(IWorldComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            if (registry.TryGetValueFactory<T>(out var factory))
            {
                builder.Add(factory.Invoke());
            }
            else
            {
                builder.Add<T>();
            }
        }
    }

    public ArchetypeId WorldArchetype { get; private set; }

    public void Register(IArchetypeRegistry registry)
    {
        WorldArchetype = registry.RegisterArchetype(
            b =>
            {
                b.Add<MetadataComponent>();
                worldComponentRegistry.Accept(new RegisterWorldComponentsCallback(b));
            }
        );
    }
}
