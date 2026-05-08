using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.Relay.Client;

/// <summary>
/// This class is responsible for scheduling ECS operations on the client side. The current implementation assumes
/// that the update loop tick will be called externally from the game code, on the appropriate thread.
/// </summary>
internal class ClientEcsUpdateLoop(Store world, ILogger logger)
{
    public event Action? OnStarted;
    public event Action? OnStopped;

    public bool IsRunning { get; private set; }
    private float _applicationTime;

    public void Start()
    {
        if (IsRunning)
        {
            logger.LogError("ECS update loop is already running");
            return;
        }

        IsRunning = true;

        logger.LogInformation("Starting ECS update loop");

        OnStarted?.Invoke();

        logger.LogInformation("ECS update loop started successfully");
    }

    public void Tick(float deltaTime)
    {
        if (!IsRunning)
        {
            logger.LogError("ECS update loop is not running. Call `Start()` first.");
            return;
        }

        _applicationTime += deltaTime;
        world.SystemRoot.Update(new UpdateTick(deltaTime, _applicationTime));
    }

    public void Wait(Task task)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
                break;

            Tick(stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();

            // FIXME: This is very ugly
            Thread.Sleep(Constants.ClientEcsUpdateRateMs);
        }
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            logger.LogError("ECS update loop is not running. Cannot stop.");
            return;
        }

        IsRunning = false;

        OnStopped?.Invoke();

        logger.LogInformation("ECS update loop stopped.");
    }

    public void AddSystem(BaseSystem system)
    {
        world.SystemRoot.Add(system);
    }

    public void AddSystem<T>()
        where T : BaseSystem, new()
    {
        world.SystemRoot.Add(new T());
    }

    public void RemoveSystem(BaseSystem system)
    {
        world.SystemRoot.Remove(system);
    }
}