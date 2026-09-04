using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

/// <summary>
/// Allows registering components with the ECS system, both local and networked.
/// </summary>
public interface IModComponentRegistry
{
    /// <summary>
    /// Registers a local (not replicated over the network) component type with the ECS system.
    /// </summary>
    /// <typeparam name="T">The type of the component to register. Must be a struct.</typeparam>
    void RegisterLocalComponent<T>() where T : struct;

    /// <summary>
    /// Registers a networked (replicated over the network) component type with the ECS system.
    /// </summary>
    /// <typeparam name="T">The type of the component to register. Must be a struct that implements <see cref="INetworkedComponent"/>.</typeparam>
    void RegisterComponent<T>() where T : struct, INetworkedComponent;
}