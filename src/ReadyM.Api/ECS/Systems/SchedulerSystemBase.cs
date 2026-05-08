using System.Threading;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;

namespace ReadyM.Api.ECS.Systems;

internal abstract class SchedulerSystemBase(ILogger logger) : BaseSystem
{
    private readonly PendingActionUpdater<CommandBufferSynced> _scheduler = new(null!, logger);

    public PendingActionScheduler<CommandBufferSynced> Scheduler => _scheduler;

    protected override void OnAddStore(EntityStore store)
    {
        var cb = store.GetCommandBuffer();
        cb.ReuseBuffer = true;
        var commandBuffer = cb.Synced;
        _scheduler.SetContext(commandBuffer);
        _scheduler.SetThread(Thread.CurrentThread);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        _scheduler.SetContext(null!);
    }

    protected override void OnUpdateGroup()
    {
        _scheduler.Update();
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