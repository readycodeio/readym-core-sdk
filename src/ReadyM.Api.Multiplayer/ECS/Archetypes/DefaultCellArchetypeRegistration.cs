using System;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultCellArchetypeRegistration(ICellComponentRegistry cellComponentRegistry) : IArchetypeRegistration
{
    private class RegisterCellComponentsCallback(ArchetypeBuilder builder) : ICellComponentRegistryCallback
    {
        public void AcceptModComponent(ICellComponentRegistry registry, ModComponentRegistration registration, string typeFullName)
            => throw new NotSupportedException(
                $"{nameof(AcceptModComponent)} is not supported here: the cell archetype is fixed at build time, and a mod cannot add to it. "
                + $"Offending component: {typeFullName}.");

        public void AcceptComponent<T>(ICellComponentRegistry registry, T defaultValue = default)
            where T : struct, IComponent
        {
            builder.Add<T>();
        }
    }

    public ArchetypeId CellArchetype { get; private set; }

    public void Register(IArchetypeRegistry registry)
    {
        CellArchetype = registry.RegisterArchetype(
            new ArchetypeBuilder()
                .Add<MetadataComponent>()
                .Add<CellScopeComponent>()
                .Add<InParentAreaScopeComponent>()
                .Add<EmptyScopeDeletionComponent>()
                .AddTag<ScopeEntityTag>()
                .With(b => cellComponentRegistry.Accept(new RegisterCellComponentsCallback(b)))
        );
    }
}
