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
    CommandBufferSynced CommandBuffer { get; }
    bool IsRunning { get; }
    
    event Action? OnStarted;
    event Action? OnStopped;
    event Action<CommandBufferSynced>? OnUpdateLoop;

    void Start();
    void Stop();
    void AddSystem(BaseSystem system);
    void AddSystem<T>()
        where T : BaseSystem, new();
    void RemoveSystem(BaseSystem system);

    /// <summary>
    /// Executes one tick of the ECS update loop. Should be called regularly, e.g. from a game loop or a timer.
    /// </summary>
    /// <param name="deltaTime">Time in seconds from the previous tick.</param>
    void Tick(float deltaTime);
    void Wait(Task task);
}