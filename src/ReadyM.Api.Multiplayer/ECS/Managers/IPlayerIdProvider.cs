using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

public interface IPlayerIdProvider
{
    PlayerId? PlayerId { get; }
}