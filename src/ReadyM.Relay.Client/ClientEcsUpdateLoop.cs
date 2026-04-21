using System;
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
public class ClientEcsUpdateLoop
{
    public readonly Store World;

    private readonly ILogger _logger;

    public event Action? OnStarted;
    public event Action? OnStopped;

    public bool IsRunning { get; private set; }
    private float _applicationTime = 0f;

    public ClientEcsUpdateLoop(Store world, ILogger logger)
    {
        World = world;
        _logger = logger;
    }

    public void Start()
    {
        if (IsRunning)
        {
            _logger.LogError("ECS update loop is already running");
            return;
        }

        IsRunning = true;

        _logger.LogInformation("Starting ECS update loop");

        OnStarted?.Invoke();

        _logger.LogInformation("ECS update loop started successfully");
    }

    public void Tick(float deltaTime)
    {
        if (!IsRunning)
        {
            _logger.LogError("ECS update loop is not running. Call `Start()` first.");
            return;
        }

        _applicationTime += deltaTime;
        World.SystemRoot.Update(new UpdateTick(deltaTime, _applicationTime));
    }

    public void Wait(Task task)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
            _logger.LogError("ECS update loop is not running. Cannot stop.");
            return;
        }

        IsRunning = false;

        OnStopped?.Invoke();

        _logger.LogInformation("ECS update loop stopped.");
    }

    public void AddSystem(BaseSystem system)
    {
        World.SystemRoot.Add(system);
    }

    public void AddSystem<T>()
        where T : BaseSystem, new()
    {
        World.SystemRoot.Add(new T());
    }

    public void RemoveSystem(BaseSystem system)
    {
        World.SystemRoot.Remove(system);
    }
}