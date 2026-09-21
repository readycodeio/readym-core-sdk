using System;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultPlayerArchetypeRegistration(
    IPlayerComponentRegistry playerComponentRegistry,
    IModArchetypeExtensions modExtensions,
    IModComponentStrides strides) : IArchetypeRegistration
{
    private class RegisterPlayerComponentsCallback(ArchetypeBuilder builder) : IPlayerComponentRegistryCallback
    {
        public void AcceptComponent<T>(IPlayerComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add(defaultValue);
        }
    }

    private IArchetypeRegistry? _registry;
    private ArchetypeId? _built;
    
    public ArchetypeId PlayerArchetype => _built ??= Build();

    public void Register(IArchetypeRegistry registry) => _registry = registry;

    private ArchetypeId Build()
    {
        if (_registry is null)
            throw new InvalidOperationException("The player archetype was asked for before it was registered with a store.");

        return _registry.RegisterArchetype(new ArchetypeBuilder()
            .Add<MetadataComponent>()
            .Add<PlayerScopeComponent>()
            .AddTag<ScopeEntityTag>()
            .With(b => playerComponentRegistry.Accept(new RegisterPlayerComponentsCallback(b)))
                .With(AddModComponents)
        );
    }

    private void AddModComponents(ArchetypeBuilder builder)
    {
        foreach (var component in modExtensions.For(WellKnownArchetype.Player))
        {
            var (structIndex, stride) = strides.Of(component);
            builder.Add(structIndex, stride);
        }
    }
}
