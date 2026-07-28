using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

public interface IComponentRegistry
{
    int RegisterLocalComponent<T>() where T : struct;

    /// <inheritdoc cref="RegisterLocalComponent{T}"/>
    int RegisterComponent<T>() where T : struct, INetworkedComponent;
}