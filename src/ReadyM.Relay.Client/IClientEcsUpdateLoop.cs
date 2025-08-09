using System;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Client.Blobs;

namespace ReadyM.Relay.Client;

public interface IClientEcsUpdateLoop
{
    PendingActionScheduler<CommandBufferSynced> Scheduler { get; }
    event Action<CommandBufferSynced>? OnUpdateLoop;
    bool IsRunning { get; }
    
    CommandBufferSynced CommandBuffer { get; }
    
    void Start();
    void Stop();
    void AddSystem(BaseSystem system);
    void AddSystem<T>()
        where T : BaseSystem, new();
    void RemoveSystem(BaseSystem system);

    void Tick(UpdateTick tick);
    void Wait(Task task);
}