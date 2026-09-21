using System;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultWorldArchetypeRegistration(
    IWorldComponentRegistry worldComponentRegistry,
    IModArchetypeExtensions modExtensions,
    IModComponentStrides strides) : IArchetypeRegistration
{
    private class RegisterWorldComponentsCallback(ArchetypeBuilder builder) : IWorldComponentRegistryCallback
    {
        public void AcceptComponent<T>(IWorldComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add(defaultValue);
        }
    }

    private IArchetypeRegistry? _registry;
    private ArchetypeId? _built;
    
    public ArchetypeId WorldArchetype => _built ??= Build();

    public void Register(IArchetypeRegistry registry) => _registry = registry;

    private ArchetypeId Build()
    {
        if (_registry is null)
            throw new InvalidOperationException(
                "The world archetype was asked for before it was registered with a store.");

        return _registry.RegisterArchetype(new ArchetypeBuilder()
            .Add<MetadataComponent>()
            .AddTag<ScopeEntityTag>() // FIXME (Kuba): The world entity is not a scope, this tag here is to prevent being included in ownership transfer queries
            .With(b => worldComponentRegistry.Accept(new RegisterWorldComponentsCallback(b)))
            .With(AddModComponents));
    }

    private void AddModComponents(ArchetypeBuilder builder)
    {
        foreach (var component in modExtensions.For(WellKnownArchetype.World))
        {
            var (structIndex, stride) = strides.Of(component);
            builder.Add(structIndex, stride);
        }
    }
}
