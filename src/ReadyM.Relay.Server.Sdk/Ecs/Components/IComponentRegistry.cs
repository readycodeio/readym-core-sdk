namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

public interface IComponentRegistry
{
    int RegisterComponent<T>() where T : unmanaged;
}