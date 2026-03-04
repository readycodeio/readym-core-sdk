using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.Relay.Client;

/// <summary>
/// This class is responsible for scheduling ECS operations on the client side. The current implementation assumes
/// that the update loop tick will be called externally from the game code, on the appropriate thread.
/// </summary>
public class ClientEcsUpdateLoop : IClientEcsUpdateLoop
{
    public readonly Store World;
    public CommandBufferSynced CommandBuffer { get; }

    private readonly ILogger _logger;

    private readonly PendingActionUpdater<CommandBufferSynced> _scheduler;

    public PendingActionScheduler<CommandBufferSynced> Scheduler => _scheduler;

    public event Action<CommandBufferSynced>? OnUpdateLoop;
    public event Action? OnStarted;
    public event Action? OnStopped;

    public bool IsRunning { get; private set; }
    private float _applicationTime = 0f;

    public ClientEcsUpdateLoop(Store world, ILogger logger)
    {
        World = world;
        var cb = World.GetCommandBuffer();
        cb.ReuseBuffer = true;
        CommandBuffer = cb.Synced;

        _logger = logger;
        _scheduler = new PendingActionUpdater<CommandBufferSynced>(CommandBuffer, logger);
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

        _scheduler.SetThread(Thread.CurrentThread);

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

        try
        {
            CommandBuffer.Playback();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error during CommandBuffer playback");
        }

        _applicationTime += deltaTime;
        World.SystemRoot.Update(new UpdateTick(deltaTime, _applicationTime));
        _scheduler.Update();

        OnUpdateLoop?.Invoke(CommandBuffer);
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
            _logger.LogError("ECS update loop is not running. Cannot stop.");
            return;
        }

        IsRunning = false;
        _scheduler.SetThread(null);

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