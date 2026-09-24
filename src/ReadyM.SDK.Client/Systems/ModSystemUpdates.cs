using Friflo.Engine.ECS.Systems;
using ReadyM.Api.DI;
using ReadyM.SDK.Systems;
using SdkTick = ReadyM.SDK.Systems.Tick;

namespace ReadyM.SDK.Client.Systems;

/// Wraps our [System]s into a Friflo system.
public sealed class ModSystemUpdates : BaseSystem
{
    private readonly IDependencyContainer _services;

    private IReadOnlyList<IModSystem>? _systems;

    private ulong _count;

    public ModSystemUpdates(IDependencyContainer services)
    {
        _services = services;

        SystemRegistry.RegisterAll(services);
    }

    public override string Name => "Mod systems";

    protected internal override void OnUpdateGroup()
    {
        if (!ModSystems.Running)
            return;

        _systems ??= SystemRegistry.Resolve(_services).ToList();

        var moment = new SdkTick(Tick.deltaTime, Tick.time, _count++);

        foreach (var system in _systems)
            system.Update(in moment);
    }
}