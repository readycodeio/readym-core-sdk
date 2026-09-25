using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Core;

[Archetype]
[ExplicitComponent(typeof(AreaScopeComponent))]
public readonly partial struct Area : IScope
{
    [Index]
    public partial AreaId AreaId { get; set; }
    public partial PlayerId MasterClient { get; set; }
}