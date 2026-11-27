using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Server.Entities;

namespace ReadyM.SDK.Server.Events;

/// <summary>Raises events from server code, running their stages here and now.</summary>
internal sealed class ServerNetworkedEvents(IDependencyContainer container, IEntityApi entityApi) : INetworkedEvents
{
    public INetworkedEventRaise Raise<T>(T networkEvent) where T : struct, INetworkedEvent
    {
        RaiseAs(PlayerId.Server, networkEvent);
        return ServerRaise.Instance;
    }

    public void RaiseAs<T>(PlayerId origin, T networkEvent) where T : struct, INetworkedEvent
    {
        if (!IsOnWorldThread())
            throw new EventRaisedOffEcsThreadException(typeof(T));

        container.Resolve<ServerNetworkedEventCoordinator<T>>().Raise(networkEvent, origin);
    }

    public void RaiseIn<T>(DimensionId dimension, T networkEvent) where T : struct, INetworkedEvent
    {
        if (!IsOnWorldThread())
            throw new EventRaisedOffEcsThreadException(typeof(T));

        container.Resolve<ServerNetworkedEventCoordinator<T>>().RaiseIn(networkEvent, PlayerId.Server, dimension);
    }

    private bool IsOnWorldThread()
        => entityApi is ServerEntityApi server
            ? server.IsOnWorldThread
            : throw new InvalidOperationException(
                $"{entityApi.GetType().Name} cannot tell which thread the world runs on.");

    /// <summary>The server needs no prediction: it runs the stages itself.</summary>
    private sealed class ServerRaise : INetworkedEventRaise
    {
        public static ServerRaise Instance { get; } = new();

        public INetworkedEventRaise Predicting(Action prediction)
            => throw new InvalidOperationException("Only a client predicts a networked event.");

        public INetworkedEventRaise Rollback(Action rollback) => Predicting(rollback);
    }
}
