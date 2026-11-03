using Friflo.Engine.ECS.Systems;
using ReadyM.Api.DI;
using ReadyM.SDK.Client.Events;
using ReadyM.SDK.Events;
using ReadyM.SDK.Services;

namespace ReadyM.SDK.Client.Systems;

/// <summary>Wraps the [Service]s that declared an update into a Friflo system.</summary>
internal sealed class ModSystemUpdates : BaseSystem
{
    private readonly IDependencyContainer _services;

    private IReadOnlyList<IUpdatingService>? _updating;

    private ulong _count;

    private List<ClientNetworkedEvents>? _events;

    private List<PredictedRaiseRegistry>? _predictedRaises;

    public ModSystemUpdates(IDependencyContainer services)
    {
        if (!ServiceRegistry.RegisteredFor(services))
        {
            throw new InvalidOperationException(
                "The services the mods declared are not registered on this container. Call "
                + nameof(DependencyInjectionExtensions.RegisterModDeclarations)
                + " once the mod assemblies are loaded and before their entry points run.");
        }

        _services = services;
    }

    public override string Name => "Mod systems";

    protected internal override void OnUpdateGroup()
    {
        if (!ModSystems.Running)
            return;

        _updating ??= ServiceRegistry.Resolve(_services);

        var moment = new UpdateTime(Tick.deltaTime, Tick.time, _count++);

        // Before the mods, so a prediction that timed out is rolled back before any of them reads anything.
        // Asked for as a set, because a host that wired no event raising has none.
        _predictedRaises ??= _services.ResolveAll<PredictedRaiseRegistry>().ToList();
        _events ??= _services.ResolveAll<INetworkedEvents>().OfType<ClientNetworkedEvents>().ToList();

        foreach (var events in _events)
            events.Now = Tick.time;

        foreach (var predictedRaises in _predictedRaises)
            predictedRaises.RollBackExpired(Tick.time);

        foreach (var service in _updating)
            service.Update(in moment);
    }
}