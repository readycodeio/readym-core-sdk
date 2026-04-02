using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

public interface IPlayerIdProvider
{
    PlayerId? PlayerId { get; }
}