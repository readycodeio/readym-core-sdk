using System;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultCellArchetypeRegistration(
    ICellComponentRegistry cellComponentRegistry,
    IModArchetypeExtensions modExtensions,
    IModComponentStrides strides) : IArchetypeRegistration
{
    private class RegisterCellComponentsCallback(ArchetypeBuilder builder) : ICellComponentRegistryCallback
    {
        public void AcceptComponent<T>(ICellComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add<T>();
        }
    }

    private IArchetypeRegistry? _registry;
    private ArchetypeId? _built;
    
    public ArchetypeId CellArchetype => _built ??= Build();

    public void Register(IArchetypeRegistry registry) => _registry = registry;

    private ArchetypeId Build()
    {
        if (_registry is null)
            throw new InvalidOperationException("The cell archetype was asked for before it was registered with a store.");

        return _registry.RegisterArchetype(
            new ArchetypeBuilder()
                .Add<MetadataComponent>()
                .Add<CellScopeComponent>()
                .Add<InParentAreaScopeComponent>()
                .Add<EmptyScopeDeletionComponent>()
                .AddTag<ScopeEntityTag>()
                .With(b => cellComponentRegistry.Accept(new RegisterCellComponentsCallback(b)))
                .With(AddModComponents)
        );
    }

    private void AddModComponents(ArchetypeBuilder builder)
    {
        foreach (var component in modExtensions.For(WellKnownArchetype.Cell))
        {
            var (structIndex, stride) = strides.Of(component);
            builder.Add(structIndex, stride);
        }
    }
}
