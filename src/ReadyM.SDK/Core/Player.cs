using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Core;

[Archetype]
[ExplicitComponent(typeof(PlayerScopeComponent))]
public readonly partial struct Player : IScope
{
    public partial PlayerId PlayerId { get; set; }
}