using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Client.Relay;
using ReadyM.SDK.Client.Relay.State;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Client.Events;

/// <summary>Hands mods this client's connection as the client state processes it, on the ECS thread.</summary>
internal sealed class ClientEvents : IClientEvents, IDisposable
{
    public event Action<Player>? OnConnected;
    public event Action<DisconnectedReason, bool>? OnDisconnected;
    public event Action<int>? OnReconnecting;

    private readonly ClientState _state;
    private readonly IEntityApi _entities;
    private readonly IReconnectPolicy _reconnect;
    private readonly ILogger _logger;

    public ClientEvents(ClientState state, IEntityApi entities, IReconnectPolicy reconnect, ILogger logger)
    {
        _state = state;
        _entities = entities;
        _reconnect = reconnect;
        _logger = logger;

        _state.OnConnected += OnStateConnected;
        _state.OnDisconnected += OnStateDisconnected;
        _state.OnReconnecting += OnStateReconnecting;
        _state.OnReconnectFailed += OnStateReconnectFailed;
    }

    public void Dispose()
    {
        _state.OnReconnectFailed -= OnStateReconnectFailed;
        _state.OnReconnecting -= OnStateReconnecting;
        _state.OnDisconnected -= OnStateDisconnected;
        _state.OnConnected -= OnStateConnected;
    }

    private void OnStateConnected(PlayerId playerId, Entity playerEntity)
    {
        var player = Shape<Player>(playerEntity);
        Raise(nameof(OnConnected), () => OnConnected?.Invoke(player));
    }

    private void OnStateDisconnected(PlayerId playerId, Entity? playerEntity, DisconnectedReason reason)
        => Raise(nameof(OnDisconnected), () => OnDisconnected?.Invoke(reason, _reconnect.WillReconnectAfter(reason)));

    private void OnStateReconnecting(int attempt)
        => Raise(nameof(OnReconnecting), () => OnReconnecting?.Invoke(attempt));

    private void OnStateReconnectFailed(DisconnectedReason reason)
        => Raise(nameof(OnDisconnected), () => OnDisconnected?.Invoke(reason, false));

    // A throw must not reach the client state, which would raise the same event again on every tick
    private void Raise(string name, Action raise)
    {
        try
        {
            raise();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Raising {Event} to mods failed", name);
        }
    }

    private T Shape<T>(Entity entity) where T : struct, IArchetypeQueryable
        => new() { Handle = new EntityHandle(entity.RawEntity, _entities) };
}
