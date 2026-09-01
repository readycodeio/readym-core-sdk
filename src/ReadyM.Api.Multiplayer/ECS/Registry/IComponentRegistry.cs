using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

/// <summary>
/// Allows registering components with the ECS system, both local and networked.
/// </summary>
public interface IComponentRegistry
{
    /// <summary>
    /// Registers a networked (replicated over the network) component type with the ECS system.
    /// </summary>
    /// <typeparam name="T">The type of the component to register. Must be a struct that implements <see cref="INetworkedComponent"/>.</typeparam>
    IComponentRegistry RegisterComponent<T>() where T : struct, INetworkedComponent;
}