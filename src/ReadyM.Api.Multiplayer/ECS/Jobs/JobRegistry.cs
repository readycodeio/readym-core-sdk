using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Extensions;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal class JobRegistry
{
    private class RegisterJobsCallback(JobRegistry owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptComponent<T>(INetworkedComponentRegistry registry, T defaultValue = default)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();

            owner.Logger.LogDebug("Registering jobs for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner.RegisterApplyDeltaJob(id, new ApplyDeltaJob<T>(owner.NetEntity, owner.PlayerIdProvider));
            owner.RegisterApplySnapshotJob(id, new ApplySnapshotJob<T>(owner.NetEntity));
            owner.RegisterWriteSnapshotJob(id, new WriteSnapshotJob<T>(id), new WriteSnapshotJob<T>(id));
        }
    }

    protected readonly INetworkedEntityManager NetEntity;
    protected readonly IPlayerIdProvider PlayerIdProvider;
    protected readonly ILogger Logger;

    protected readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> ApplyDeltaJobs = [];
    protected readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> ApplySnapshotJobs = [];
    protected readonly Dictionary<NetworkedComponentId, IJob<EntityStore, QueryFilter, Entity?, NetDataWriter>> WriteSnapshotJobs = [];
    protected readonly Dictionary<NetworkedComponentId, IJob<Entity, NetDataWriter>> WriteOneSnapshotJobs = [];

    public event Action? OnApplySnapshot;

    public JobRegistry(
        INetworkedComponentRegistry registry,
        INetworkedEntityManager netEntity,
        IPlayerIdProvider playerIdProvider,
        ILogger logger)
    {
        NetEntity = netEntity;
        PlayerIdProvider = playerIdProvider;
        Logger = logger;

        registry.Accept(new RegisterJobsCallback(this));
    }

    protected void RegisterApplyDeltaJob(NetworkedComponentId componentId, IJob<NetDataReader> job)
    {
        ApplyDeltaJobs.Add(componentId, job);
    }

    protected void RegisterApplySnapshotJob(NetworkedComponentId componentId, IJob<NetDataReader> job)
    {
        ApplySnapshotJobs.Add(componentId, job);
    }

    protected void RegisterWriteSnapshotJob(
        NetworkedComponentId componentId,
        IJob<EntityStore, QueryFilter, Entity?, NetDataWriter> job,
        IJob<Entity, NetDataWriter> oneJob)
    {
        WriteSnapshotJobs.Add(componentId, job);
        WriteOneSnapshotJobs.Add(componentId, oneJob);
    }

    public void WriteSnapshot(EntityStore world, QueryFilter filter, Entity? scopeEntity, NetDataWriter writer)
    {
        foreach (var job in WriteSnapshotJobs.Values)
        {
            job.Execute(world, filter, scopeEntity, writer);
        }
    }

    public void WriteSnapshot(Entity entity, NetDataWriter writer)
    {
        foreach (var job in WriteOneSnapshotJobs.Values)
        {
            job.Execute(entity, writer);
        }
    }

    public void ApplyDelta(NetDataReader reader)
    {
        var componentId = reader.Get<NetworkedComponentId>();
        if (!ApplyDeltaJobs.TryGetValue(componentId, out var job))
        {
            Logger.LogError("No reader job registered for component ID: {Id}", componentId);
            return;
        }

        job.Execute(reader);
    }

    public void ApplySnapshot(NetDataReader reader)
    {
        while (reader.TryGetNetworkedComponentId(out var componentId))
        {
            if (!ApplySnapshotJobs.TryGetValue(componentId, out var readerJob))
            {
                Logger.LogError("No snapshot reader job registered for component ID: {Id}", componentId);
                break;
            }

            readerJob.Execute(reader);
        }

        OnApplySnapshot?.Invoke();
    }
}