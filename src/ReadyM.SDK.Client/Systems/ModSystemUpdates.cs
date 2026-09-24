using Friflo.Engine.ECS.Systems;
using ReadyM.Api.DI;
using ReadyM.SDK.Services;

namespace ReadyM.SDK.Client.Systems;

/// Wraps the [Service]s that declared an update into a Friflo system.
public sealed class ModSystemUpdates : BaseSystem
{
    private readonly IDependencyContainer _services;

    private IReadOnlyList<IUpdatingService>? _updating;

    private ulong _count;

    public ModSystemUpdates(IDependencyContainer services)
    {
        _services = services;

        ServiceRegistry.RegisterAll(services);
    }

    public override string Name => "Mod systems";

    protected internal override void OnUpdateGroup()
    {
        if (!ModSystems.Running)
            return;

        _updating ??= ServiceRegistry.Resolve(_services);

        var moment = new UpdateTime(Tick.deltaTime, Tick.time, _count++);

        foreach (var service in _updating)
            service.Update(in moment);
    }
}