namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

/// <summary>
/// Visits each component a mod declared. The mod-side counterpart of the native registries' callback: it
/// carries the component's type as a generic argument, so an acceptor can do typed work without reflection.
/// </summary>
/// <remarks>
/// An acceptor registered as a filter sees every component, whether it was declared before or after the filter
/// went in. See <see cref="ModComponentRegistry.RegisterFilter"/>.
/// </remarks>
internal interface IModComponentRegistryCallback
{
    void AcceptComponent<T>(ModComponentRegistry registry) where T : struct;
}
