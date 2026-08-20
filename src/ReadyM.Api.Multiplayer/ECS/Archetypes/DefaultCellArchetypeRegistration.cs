using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal sealed class DefaultCellArchetypeRegistration : IArchetypeRegistration
{
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
        );
    }
}
