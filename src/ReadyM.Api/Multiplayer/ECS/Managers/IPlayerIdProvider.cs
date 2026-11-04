using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

/// <exclude />
public interface IPlayerIdProvider
{
    PlayerId? PlayerId { get; }
}