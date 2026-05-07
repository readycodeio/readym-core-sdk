using System;
using Friflo.Engine.ECS.Systems;

namespace ReadyM.Api.ECS.Systems;

public class SchedulerSystemGroup : SystemGroup, IDisposable
{
    private readonly SchedulerSystemBase schedulerSystem;
    private bool _insideDelay;

    public SchedulerSystemGroup(string name, SchedulerSystemBase schedulerSystem) : base(name)
    {
        this.schedulerSystem = schedulerSystem;
        SafeBeginDelay();
    }

    // TODO: Not supported at the moment
    public void Dispose()
    {
        SafeEndDelay();
    }

    protected override void OnUpdateGroupBegin()
    {
        SafeEndDelay();
    }

    protected override void OnUpdateGroupEnd()
    {
        SafeBeginDelay();
    }
    
    private void SafeBeginDelay()
    {
        if (_insideDelay)
            return;
        
        schedulerSystem.BeginDelay();
        _insideDelay = true;
    }

    private void SafeEndDelay()
    {
        if (!_insideDelay)
            return;
        
        schedulerSystem.EndDelay();
        _insideDelay = false;
    }
}