using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Core;

[Archetype]
[ExplicitComponent(typeof(PlayerScopeComponent))]
public readonly partial struct Player
{
    public partial PlayerId PlayerId { get; set; }
}