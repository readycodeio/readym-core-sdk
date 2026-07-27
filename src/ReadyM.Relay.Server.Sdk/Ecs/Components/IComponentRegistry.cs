using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

public interface IComponentRegistry
{
    /// <param name="displayName">Label shown for this component in the server's metrics tab.
    /// Defaults to the type name; pass one only when that is not what you want listed.</param>
    int RegisterLocalComponent<T>(string? displayName = null) where T : struct;

    /// <inheritdoc cref="RegisterLocalComponent{T}"/>
    int RegisterComponent<T>(string? displayName = null) where T : struct, INetworkedComponent;
}