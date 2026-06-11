using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Compat;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Serialization;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal sealed class JobRegistry
{
    private class RegisterJobsCallback(JobRegistry owner) : INetworkedComponentRegistryCallback
    {
        public void AcceptComponent<T>(INetworkedComponentRegistry registry, T defaultValue = default)
            where T : struct, INetworkedComponent
        {
            var id = registry.GetNetworkedComponentId<T>();

            owner.Logger.LogTrace("Registering jobs for: {ComponentType} with ID {Id}", typeof(T).Name, id);
            owner.RegisterApplyDeltaJob(id, new ApplyDeltaJob<T>(owner.NetEntity, owner.PlayerIdProvider));
            owner.RegisterApplySnapshotJob(id, new ApplySnapshotJob<T>(owner.NetEntity));
            owner.RegisterWriteSnapshotJob(id, new WriteSnapshotJob<T>(id));
        }
    }

    private readonly INetworkedEntityManager NetEntity;
    private readonly IPlayerIdProvider PlayerIdProvider;
    private readonly ILogger Logger;
    private readonly INetworkedComponentRegistry _registry;

    private readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> ApplyDeltaJobs = [];
    private readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> ApplySnapshotJobs = [];
    private readonly Dictionary<NetworkedComponentId, IJob<EntityStore, QueryFilter, Entity?, NetDataWriter>> WriteSnapshotJobs = [];
    private readonly Dictionary<NetworkedComponentId, IJob<EntityStore, QueryFilter, Entity?, SpanDataWriter>> PluginWriteSnapshotJobs = [];

    public event Action? OnApplySnapshot;
    private readonly Dictionary<NetworkedComponentId, Action?> _onApplySnapshotByComponentId = [];
    private readonly Dictionary<NetworkedComponentId, Action?> _onApplyDeltaByComponentId = [];

    public void AddApplySnapshotCallback(NetworkedComponentId componentId, Action? callback)
        => _onApplySnapshotByComponentId[componentId] = (Action?)Delegate.Combine(_onApplySnapshotByComponentId.GetValueOrDefault(componentId), callback);

    public void AddApplySnapshotCallback(Type type, Action? callback)
    {
        var componentId = _registry.GetNetworkedComponentId(type);
        AddApplySnapshotCallback(componentId, callback);
    }

    public void AddApplySnapshotCallback<T>(Action? callback)
        where T : struct, INetworkedComponent
    {
        var componentId = _registry.GetNetworkedComponentId<T>();
        AddApplySnapshotCallback(componentId, callback);
    }

    public void RemoveApplySnapshotCallback(NetworkedComponentId componentId, Action? callback)
        => _onApplySnapshotByComponentId[componentId] = (Action?)Delegate.Remove(_onApplySnapshotByComponentId.GetValueOrDefault(componentId), callback);

    public void RemoveApplySnapshotCallback(Type type, Action? callback)
    {
        var componentId = _registry.GetNetworkedComponentId(type);
        RemoveApplySnapshotCallback(componentId, callback);
    }

    public void RemoveApplySnapshotCallback<T>(Action? callback)
        where T : struct, INetworkedComponent
    {
        var componentId = _registry.GetNetworkedComponentId<T>();
        RemoveApplySnapshotCallback(componentId, callback);
    }

    public void AddApplyDeltaCallback(NetworkedComponentId componentId, Action? callback)
        => _onApplyDeltaByComponentId[componentId] = (Action?)Delegate.Combine(_onApplyDeltaByComponentId.GetValueOrDefault(componentId), callback);

    public void AddApplyDeltaCallback(Type type, Action? callback)
    {
        var componentId = _registry.GetNetworkedComponentId(type);
        AddApplyDeltaCallback(componentId, callback);
    }

    public void AddApplyDeltaCallback<T>(Action? callback)
        where T : struct, INetworkedComponent
    {
        var componentId = _registry.GetNetworkedComponentId<T>();
        AddApplyDeltaCallback(componentId, callback);
    }

    public void RemoveApplyDeltaCallback(NetworkedComponentId componentId, Action? callback)
        => _onApplyDeltaByComponentId[componentId] = (Action?)Delegate.Remove(_onApplyDeltaByComponentId.GetValueOrDefault(componentId), callback);

    public void RemoveApplyDeltaCallback(Type type, Action? callback)
    {
        var componentId = _registry.GetNetworkedComponentId(type);
        RemoveApplyDeltaCallback(componentId, callback);
    }

    public void RemoveApplyDeltaCallback<T>(Action? callback)
        where T : struct, INetworkedComponent
    {
        var componentId = _registry.GetNetworkedComponentId<T>();
        RemoveApplyDeltaCallback(componentId, callback);
    }

    public JobRegistry(
        INetworkedComponentRegistry registry,
        INetworkedEntityManager netEntity,
        IPlayerIdProvider playerIdProvider,
        ILogger logger)
    {
        _registry = registry;
        NetEntity = netEntity;
        PlayerIdProvider = playerIdProvider;
        Logger = logger;

        _registry.Accept(new RegisterJobsCallback(this));
    }

    private void RegisterApplyDeltaJob(NetworkedComponentId componentId, IJob<NetDataReader> job)
    {
        ApplyDeltaJobs.Add(componentId, job);
    }

    private void RegisterApplySnapshotJob(NetworkedComponentId componentId, IJob<NetDataReader> job)
    {
        ApplySnapshotJobs.Add(componentId, job);
    }

    private void RegisterWriteSnapshotJob(
        NetworkedComponentId componentId,
        IJob<EntityStore, QueryFilter, Entity?, NetDataWriter> job)
    {
        WriteSnapshotJobs.Add(componentId, job);
    }
    
    internal void RegisterWriteSnapshotJob(NetworkedComponentId componentId, 
        IJob<EntityStore, QueryFilter, Entity?, SpanDataWriter> job)
    {
        PluginWriteSnapshotJobs.Add(componentId, job);
    }

    public void WriteSnapshot(EntityStore world, QueryFilter filter, Entity? scopeEntity, SpanDataWriter writer)
    {
        foreach (var job in WriteSnapshotJobs.Values)
        {
            job.Execute(world, filter, scopeEntity, writer);
        }

        foreach (var pluginJob in PluginWriteSnapshotJobs.Values)
        {
            pluginJob.Execute(world, filter, scopeEntity, writer);
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

        if (_onApplyDeltaByComponentId.TryGetValue(componentId, out var callback))
        {
            callback?.Invoke();
        }
    }

    [ThreadStatic]
    private static List<NetworkedComponentId>? _componentIds;

    public void ApplySnapshot(NetDataReader reader)
    {
        _componentIds ??= [];
        _componentIds.Clear();

        while (reader.TryGetNetworkedComponentId(out var componentId))
        {
            _componentIds.Add(componentId);

            if (!ApplySnapshotJobs.TryGetValue(componentId, out var readerJob))
            {
                Logger.LogError("No snapshot reader job registered for component ID: {Id}", componentId);
                break;
            }

            readerJob.Execute(reader);
        }

        OnApplySnapshot?.Invoke();

        foreach (var componentId in _componentIds)
        {
            if (_onApplySnapshotByComponentId.TryGetValue(componentId, out var callback))
            {
                callback?.Invoke();
            }
        }
    }
}