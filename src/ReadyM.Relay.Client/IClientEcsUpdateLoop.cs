using System;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Helpers;

namespace ReadyM.Relay.Client;

public interface IClientEcsUpdateLoop
{
    PendingActionScheduler<CommandBufferSynced> Scheduler { get; }
    event Action<CommandBufferSynced>? OnUpdateLoop;
    bool IsRunning { get; }
    
    Task StartAsync(CancellationToken token);
    Task RunAsync(CancellationToken token);
    void Stop();
    void AddSystem(BaseSystem system);
    void AddSystem<T>()
        where T : BaseSystem, new();
    void RemoveSystem(BaseSystem system);
}