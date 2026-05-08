using Friflo.Engine.ECS.Systems;

namespace ReadyM.Api.ECS.Systems;

internal class SchedulerSystemGroup : SystemGroup
{
    private readonly SchedulerSystemBase schedulerSystem;

    public SchedulerSystemGroup(string name, SchedulerSystemBase schedulerSystem) : base(name)
    {
        this.schedulerSystem = schedulerSystem;
        schedulerSystem.BeginDelay();
    }

    protected override void OnUpdateGroupBegin()
    {
        schedulerSystem.EndDelay();
    }

    protected override void OnUpdateGroupEnd()
    {
        schedulerSystem.BeginDelay();
    }
}