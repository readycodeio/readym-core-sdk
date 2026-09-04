using System;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultPlayerArchetypeRegistration(IPlayerComponentRegistry playerComponentRegistry) : IArchetypeRegistration
{
    private class RegisterPlayerComponentsCallback(ArchetypeBuilder builder) : IPlayerComponentRegistryCallback
    {
        public void AcceptModComponent(IPlayerComponentRegistry registry, ModComponentInfo registration, string typeFullName)
            => throw new NotSupportedException(
                $"{nameof(AcceptModComponent)} is not supported here: the player archetype is fixed at build time, and a mod cannot add to it. "
                + $"Offending component: {typeFullName}.");

        public void AcceptComponent<T>(IPlayerComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add(defaultValue);
        }
    }

    public ArchetypeId PlayerArchetype { get; private set; }

    public void Register(IArchetypeRegistry registry)
    {
        PlayerArchetype = registry.RegisterArchetype(new ArchetypeBuilder()
            .Add<MetadataComponent>()
            .Add<PlayerScopeComponent>()
            .AddTag<ScopeEntityTag>()
            .With(b => playerComponentRegistry.Accept(new RegisterPlayerComponentsCallback(b))));
    }
}
