using System.Threading;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;

namespace ReadyM.Api.ECS.Systems;

public abstract class SchedulerSystemBase(ILogger logger) : BaseSystem
{
    private readonly PendingActionUpdater<CommandBufferSynced> _scheduler = new(null!, logger);
    private CommandBuffer? _commandBuffer;

    public PendingActionScheduler<CommandBufferSynced> Scheduler => _scheduler;

    protected override void OnAddStore(EntityStore store)
    {
        _commandBuffer = store.GetCommandBuffer();
        _commandBuffer.ReuseBuffer = true;
        var commandBuffer = _commandBuffer.Synced;
        _scheduler.SetContext(commandBuffer);
        _scheduler.SetThread(Thread.CurrentThread);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        _scheduler.SetContext(null!);
        _commandBuffer!.ReturnBuffer();
        _commandBuffer = null;
    }

    protected override void OnUpdateGroup()
    {
        _scheduler.Update();
        _commandBuffer!.Playback();
    }

    public void BeginDelay()
    {
        _scheduler.BeginDelay();
    }

    public void EndDelay()
    {
        _scheduler.EndDelay();
    }
}