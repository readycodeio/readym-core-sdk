using ReadyM.Api.DI;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;

namespace ReadyM.Relay.Server.Sdk;

/// <summary>
/// Represents a dependency injection container for server-side mods.
/// Allows registering systems, apart from standard DI services.
/// </summary>
public interface IServerDependencyContainer : IDependencyContainer
{
    void RegisterSystem<TSystem>() where TSystem : ModSystemBase;
}