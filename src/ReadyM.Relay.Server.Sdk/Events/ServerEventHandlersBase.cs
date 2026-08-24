using ReadyM.Api.DI;

namespace ReadyM.Relay.Server.Sdk.Events;

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
