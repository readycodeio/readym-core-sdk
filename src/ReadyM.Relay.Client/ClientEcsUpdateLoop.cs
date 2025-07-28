using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Relay.Common.Protocol;

namespace ReadyM.Relay.Client;

public class ClientEcsUpdateLoop : IClientEcsUpdateLoop
{
    public readonly Store World;
    public CommandBufferSynced CommandBuffer { get; }

    private readonly ILogger _logger;

    private readonly Stopwatch _lastUpdate = new Stopwatch();
    
    private readonly PendingActionUpdater<CommandBuffer> _scheduler;
    
    public PendingActionScheduler<CommandBuffer> Scheduler => _scheduler;

    public bool IsRunning { get; private set; }

    public ClientEcsUpdateLoop(Store world, ILogger logger)
    {
        World = world;
        var cb = World.GetCommandBuffer();
        cb.ReuseBuffer = true;
        CommandBuffer = cb.Synced;

        _logger = logger;
        _scheduler = new(cb, logger);
    }

    public async Task StartAsync(CancellationToken token)
    {
        if (IsRunning)
        {
            _logger.LogError("ECS update loop is already running");
            return;
        }

        IsRunning = true;
        
        _logger.LogInformation("Starting ECS update loop");

        await Task.Delay(1, token);
        _scheduler.SetThread(Thread.CurrentThread);
        
        _logger.LogInformation("ECS update loop started successfully");
    }

    public async Task RunAsync(CancellationToken token)
    {
        if (!IsRunning)
        {
            _logger.LogError("ECS update loop is not running. Call `StartAsync()` first.");
            return;
        }
        
        while (!token.IsCancellationRequested)
        {
            _lastUpdate.Restart();
            
            lock (CommandBuffer)
            {
                CommandBuffer.Playback();
            }

            World.SystemRoot.Update(default);

            _scheduler.Update();
            
            // Wait for the next update cycle
            await Task.Delay(Constants.ClientEcsUpdateRateMs - (int)_lastUpdate.ElapsedMilliseconds, token);
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
        _scheduler.SetThread(null!);
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