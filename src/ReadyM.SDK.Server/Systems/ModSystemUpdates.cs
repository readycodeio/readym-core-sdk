using System.Runtime.CompilerServices;
using ReadyM.Api.DI;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.SDK.Services;

namespace ReadyM.SDK.Server.Systems;

/// Wraps the [Service]s that declared an update into a Friflo system.
[Obsolete("Obsolete")]
internal sealed class ModSystemUpdates(IDependencyContainer services) : ModSystemBase
{
    private static readonly ConditionalWeakTable<IDependencyContainer, object> Wired = new();
    private IReadOnlyList<IUpdatingService>? _updating;

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
        _updating ??= [.. ServiceRegistry.Resolve(services)];

        var moment = new UpdateTime(tick.DeltaTime, tick.Time, _count++);

        foreach (var service in _updating)
            service.Update(in moment);
    }
}
