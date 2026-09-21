using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

/// <summary>
/// Allows registering components with the ECS system, both local and networked.
/// </summary>
public interface IComponentRegistry
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
    /// <param name="delivery">How its changes travel: 0 reliable, 1 unreliable.</param>
    void RegisterComponent<T>(byte delivery = 0) where T : struct, INetworkedComponent;

    /// <summary>
    /// Registers a networked (replicated over the network) component type with the ECS system.
    /// </summary>
    /// <param name="component">The type of the component to register. Must be a struct that implements <see cref="INetworkedComponent"/>.</param>
    /// <param name="delivery">How its changes travel: 0 reliable, 1 unreliable.</param>
    void RegisterComponent(Type component, byte delivery);
}