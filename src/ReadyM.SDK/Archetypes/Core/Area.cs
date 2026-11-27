using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Archetypes.Core;

/// <summary>A scope for one area of the world, holding what is in it while players are there.</summary>
[Archetype(Replication = Replication.OwnershipBased)]
[ExplicitComponent(typeof(AreaScopeComponent))]
public readonly partial struct Area : IScope
{
    /// <summary>Which area this is, with its dimension.</summary>
    [Index]
    public partial FullAreaId FullAreaId { get; set; }
    /// <summary>The player whose game simulates what the area holds.</summary>
    public partial PlayerId MasterClient { get; set; }
}