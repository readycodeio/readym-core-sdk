using System.Runtime.CompilerServices;
using ReadyM.Api.DI;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.SDK.Systems;

namespace ReadyM.SDK.Server.Systems;

/// Wraps our [System]s into a Friflo system.
[Obsolete("Obsolete")]
internal sealed class ModSystemUpdates(IDependencyContainer services) : ModSystemBase
{
    private static readonly ConditionalWeakTable<IDependencyContainer, object> Wired = new();
    private IReadOnlyList<IModSystem>? _systems;

    private ulong _count;

    /// Registers the one updater a container needs, whichever mod asks first.
    [Obsolete("Obsolete")]
    public static void Wire(IDependencyContainer services)
    {
        if (Wired.TryGetValue(services, out _))
            return;

        Wired.Add(services, new object());
        services.RegisterSingleton<ModSystemBase>(new ModSystemUpdates(services));
    }

    protected override void OnUpdate(UpdateTick tick)
    {
        _systems ??= [.. SystemRegistry.Resolve(services)];

        var moment = new Tick(tick.DeltaTime, tick.Time, _count++);

        foreach (var system in _systems)
            system.Update(in moment);
    }
}
