using ReadyM.Api.DI;

namespace ReadyM.Relay.Server.Sdk.Events;

/// <summary>
/// Base class for server event handlers.
/// Subclasses should implement the Subscribe and Unsubscribe methods to register and unregister event handlers with the ServerEventsApi.
/// </summary>
/// <param name="events"></param>
public abstract class ServerEventHandlersBase(ServerEventsApi events) : IHostedService
{
    public void OnScopeStart()
    {
        Subscribe(events);
    }

    public virtual void Dispose()
    {
        Unsubscribe(events);
    }

    protected abstract void Subscribe(ServerEventsApi events);

    protected abstract void Unsubscribe(ServerEventsApi events);
}
