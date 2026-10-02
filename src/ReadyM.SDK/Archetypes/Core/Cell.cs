using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Archetypes.Core;

[Archetype]
[ExplicitComponent(typeof(CellScopeComponent))]
public readonly partial struct Cell : IScope
{
    [Index]
    public partial FullCellId FullCellId { get; set; }

    public partial PlayerId MasterClient { get; set; }
}
