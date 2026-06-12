using System.Collections.Generic;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Serialization;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal sealed class SerializationJobRegistry
{
    private class RegisterJobsCallback(SerializationJobRegistry owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptComponent<T>(INetworkedComponentRegistry registry, T defaultValue = default)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();

            owner._logger.LogDebug("Registering jobs for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner.RegisterApplyDeltaJob(id, new ApplyDeltaJob<T>(owner._netEntity, owner._playerIdProvider));
            owner.RegisterApplySnapshotJob(id, new ApplySnapshotJob<T>(owner._netEntity));
            owner.RegisterWriteSnapshotJob(id, new WriteSnapshotJob<T>(id));
        }
    }

    private readonly INetworkedEntityManager _netEntity;
    private readonly IPlayerIdProvider _playerIdProvider;
    private readonly ILogger _logger;

    private readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> _applyDeltaJobs = [];
    private readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> _applySnapshotJobs = [];
    private readonly Dictionary<NetworkedComponentId, IJob<EntityStore, QueryFilter, Entity?, SpanDataWriter>> _writeSnapshotJobs = [];

    public SerializationJobRegistry(
        INetworkedComponentRegistry registry,
        INetworkedEntityManager netEntity,
        IPlayerIdProvider playerIdProvider,
        ILogger logger)
    {
        _netEntity = netEntity;
        _playerIdProvider = playerIdProvider;
        _logger = logger;

        registry.Accept(new RegisterJobsCallback(this));
    }

    internal void RegisterApplyDeltaJob(NetworkedComponentId componentId, IJob<NetDataReader> job)
    {
        _applyDeltaJobs.Add(componentId, job);
    }

    internal void RegisterApplySnapshotJob(NetworkedComponentId componentId, IJob<NetDataReader> job)
    {
        _applySnapshotJobs.Add(componentId, job);
    }

    internal void RegisterWriteSnapshotJob(
        NetworkedComponentId componentId,
        IJob<EntityStore, QueryFilter, Entity?, SpanDataWriter> job)
    {
        _writeSnapshotJobs.Add(componentId, job);
    }

    public void WriteSnapshot(EntityStore world, QueryFilter filter, Entity? scopeEntity, SpanDataWriter writer)
    {
        filter = filter.FreezeFilter();
        foreach (var job in _writeSnapshotJobs.Values)
        {
            job.Execute(world, filter, scopeEntity, writer);
        }
    }

    public void ApplyDelta(NetDataReader reader)
    {
        var componentId = reader.Get<NetworkedComponentId>();
        if (!_applyDeltaJobs.TryGetValue(componentId, out var job))
        {
            _logger.LogError("No reader job registered for component ID: {Id}", componentId);
            return;
        }

        job.Execute(reader);
    }

    public void ApplySnapshot(NetDataReader reader)
    {
        while (reader.TryGetNetworkedComponentId(out var componentId))
        {
            if (!_applySnapshotJobs.TryGetValue(componentId, out var readerJob))
            {
                _logger.LogError("No snapshot reader job registered for component ID: {Id}", componentId);
                break;
            }

            readerJob.Execute(reader);
        }
    }
}