using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal class DefaultNetworkedComponentJobRegistration(
    INetworkedEntityManager netEntity,
    IPlayerIdProvider playerIdProvider,
    ILogger logger) : INetworkedComponentJobRegistration
{
    private class RegisterJobsCallback(DefaultNetworkedComponentJobRegistration owner, NetworkedComponentJobRegistry jobRegistry) : INetworkedComponentRegistryCallback
    {
        public void AcceptModComponent(INetworkedComponentRegistry registry, ModComponentInfo info, string typeFullName)
        {
            // Nothing to do here, mod component jobs are registered elsewhere
        }

        public void AcceptComponent<T>(INetworkedComponentRegistry registry, T defaultValue = default)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();

            owner._logger.LogDebug("Registering jobs for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            jobRegistry.RegisterApplyDeltaJob(id, new ApplyDeltaJob<T>(owner._netEntity, owner._playerIdProvider, owner._logger));
            jobRegistry.RegisterApplySnapshotJob(id, new ApplySnapshotJob<T>(owner._netEntity));
            jobRegistry.RegisterWriteSnapshotJob(id, new WriteSnapshotJob<T>(id));
        }
    }

    private readonly ILogger _logger = logger;
    private readonly INetworkedEntityManager _netEntity = netEntity;
    private readonly IPlayerIdProvider _playerIdProvider = playerIdProvider;

    public void Register(INetworkedComponentRegistry registry, NetworkedComponentJobRegistry jobRegistry)
    {
        registry.Accept(new RegisterJobsCallback(this, jobRegistry));
    }
}
