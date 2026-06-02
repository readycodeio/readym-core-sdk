namespace ReadyM.Relay.Server.Sdk;

public interface IComponentRegistry
{
    int RegisterComponent<T>() where T : unmanaged;
}