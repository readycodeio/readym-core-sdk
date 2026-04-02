using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

internal interface IPlayerIdProvider
{
    PlayerId? PlayerId { get; }
}