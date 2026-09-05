using System;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultWorldArchetypeRegistration(IWorldComponentRegistry worldComponentRegistry) : IArchetypeRegistration
{
    private class RegisterWorldComponentsCallback(ArchetypeBuilder builder) : IWorldComponentRegistryCallback
    {
        public void AcceptModComponent(IWorldComponentRegistry registry, ModComponentInfo info, string typeFullName)
            => throw new NotSupportedException(
                $"{nameof(AcceptModComponent)} is not supported here: the world archetype is fixed at build time, and a mod cannot add to it. "
                + $"Offending component: {typeFullName}.");

        public void AcceptComponent<T>(IWorldComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add(defaultValue);
        }
    }

    public ArchetypeId WorldArchetype { get; private set; }

    public void Register(IArchetypeRegistry registry)
    {
        WorldArchetype = registry.RegisterArchetype(new ArchetypeBuilder()
            .Add<MetadataComponent>()
            .With(b => worldComponentRegistry.Accept(new RegisterWorldComponentsCallback(b))));
    }
}
