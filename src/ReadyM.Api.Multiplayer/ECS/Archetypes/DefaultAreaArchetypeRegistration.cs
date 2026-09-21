using System;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultAreaArchetypeRegistration(
    IAreaComponentRegistry areaComponentRegistry,
    IModArchetypeExtensions modExtensions,
    IModComponentStrides strides
) : IArchetypeRegistration
{
    private class RegisterAreaComponentsCallback(ArchetypeBuilder builder) : IAreaComponentRegistryCallback
    {
        public void AcceptComponent<T>(IAreaComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add(defaultValue);
        }
    }

    private IArchetypeRegistry? _registry;
    private ArchetypeId? _built;

    public ArchetypeId AreaArchetype => _built ??= Build();

    public void Register(IArchetypeRegistry registry) => _registry = registry;

    private ArchetypeId Build()
    {
        if (_registry is null)
            throw new InvalidOperationException(
                "The area archetype was asked for before it was registered with a store.");

        return _registry.RegisterArchetype(new ArchetypeBuilder()
            .Add<MetadataComponent>()
            .Add<AreaScopeComponent>()
            .Add<EmptyScopeDeletionComponent>()
            .AddTag<ScopeEntityTag>()
            .With(b => areaComponentRegistry.Accept(new RegisterAreaComponentsCallback(b)))
            .With(AddModComponents)
        );
    }

    /// The other half: what mods declared, which the host knows only as ids.
    private void AddModComponents(ArchetypeBuilder builder)
    {
        foreach (var component in modExtensions.For(WellKnownArchetype.Area))
        {
            var (structIndex, stride) = strides.Of(component);
            builder.Add(structIndex, stride);
        }
    }
}