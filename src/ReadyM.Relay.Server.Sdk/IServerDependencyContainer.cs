using ReadyM.Api.DI;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;

namespace ReadyM.Relay.Server.Sdk;

public interface IServerDependencyContainer : IDependencyContainer
{
    void RegisterSystem<TSystem>() where TSystem : ModSystemBase;
}