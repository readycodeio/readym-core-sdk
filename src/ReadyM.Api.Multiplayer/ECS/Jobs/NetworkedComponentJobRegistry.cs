using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Compat;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Extensions;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

internal sealed class NetworkedComponentJobRegistry
{
    private readonly INetworkedComponentRegistry _registry;
    private readonly ILogger _logger;

    private readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> _applyDeltaJobs = [];
    private readonly Dictionary<NetworkedComponentId, IJob<NetDataReader>> _applySnapshotJobs = [];
    private readonly Dictionary<NetworkedComponentId, IJob<EntityStore, QueryFilter, Entity?, NetDataWriter>> _writeSnapshotJobs = [];

    public event Action? OnApplySnapshot;
    public event Action<NetworkedComponentId>? OnApplyDelta;
    private readonly Dictionary<NetworkedComponentId, Action?> _onApplySnapshotByComponentId = [];
    private readonly Dictionary<NetworkedComponentId, Action?> _onApplyDeltaByComponentId = [];

    public NetworkedComponentJobRegistry(
        INetworkedComponentRegistry registry,
        ILogger logger,
        IEnumerable<INetworkedComponentJobRegistration> registrations)
    {
        _registry = registry;
        _logger = logger;

        foreach (var registration in registrations)
        {
            registration.Register(registry, this);
        }
    }

    public void RegisterApplyDeltaJob(NetworkedComponentId componentId, IJob<NetDataReader> job)
    {
        _applyDeltaJobs.Add(componentId, job);
    }

    public void RegisterApplySnapshotJob(NetworkedComponentId componentId, IJob<NetDataReader> job)
    {
        _applySnapshotJobs.Add(componentId, job);
    }

    public void RegisterWriteSnapshotJob(NetworkedComponentId componentId, IJob<EntityStore, QueryFilter, Entity?, NetDataWriter> job)
    {
        _writeSnapshotJobs.Add(componentId, job);
    }

    public void WriteSnapshot(EntityStore world, QueryFilter filter, Entity? scopeEntity, NetDataWriter writer)
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

        if (_onApplyDeltaByComponentId.TryGetValue(componentId, out var callback))
        {
            callback?.Invoke();
        }

        OnApplyDelta?.Invoke(componentId);
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

            if (!_applySnapshotJobs.TryGetValue(componentId, out var readerJob))
            {
                _logger.LogError("No snapshot reader job registered for component ID: {Id}", componentId);
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

    public void AddApplySnapshotCallback(NetworkedComponentId componentId, Action? callback)
        => _onApplySnapshotByComponentId[componentId] = (Action?)Delegate.Combine(_onApplySnapshotByComponentId.GetValueOrDefault(componentId), callback);

    public void AddApplySnapshotCallback(Type type, Action? callback)
    {
        var componentId = _registry.GetNetworkedComponentId(type);
        AddApplySnapshotCallback(componentId, callback);
    }

    public void AddApplySnapshotCallback<T>(Action? callback)
        where T : IComponent
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
        where T : IComponent
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
        where T : IComponent
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
        where T : IComponent
    {
        var componentId = _registry.GetNetworkedComponentId<T>();
        RemoveApplyDeltaCallback(componentId, callback);
    }
}
