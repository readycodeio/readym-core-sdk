using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Archetypes.Core;

[Archetype]
[ExplicitComponent(typeof(AreaScopeComponent))]
public readonly partial struct Area : IScope
{
    [Index]
    public partial AreaId AreaId { get; set; }
    public partial PlayerId MasterClient { get; set; }
}