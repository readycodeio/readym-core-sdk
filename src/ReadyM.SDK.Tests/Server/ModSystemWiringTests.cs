using ReadyM.Api.DI;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.SDK.Server.Systems;

namespace ReadyM.SDK.Tests.Server;

/// Every server mod runs Init against the one container they share, so what Init registers has to
/// hold whether one mod asks or five do.
public class ModSystemWiringTests
{
    private sealed class Container : DependencyContainerBase;

    /// The one already running stays. The container would otherwise keep the last registration and
    /// quietly drop the first, taking its tick count and its resolved systems with it.
    [Fact]
    public void A_later_mod_does_not_replace_the_updater_already_running()
    {
        var container = new Container();

        container.Init();

        ModSystemUpdates.Wire(container);

        var running = Assert.Single(((IDependencyContainer)container).ResolveAll<ModSystemBase>());

        ModSystemUpdates.Wire(container);
        ModSystemUpdates.Wire(container);

        Assert.Same(running, Assert.Single(((IDependencyContainer)container).ResolveAll<ModSystemBase>()));
    }

    /// Two games in one process are two containers, and each needs its own.
    [Fact]
    public void One_for_each_container()
    {
        var first = new Container();
        var second = new Container();

        first.Init();
        second.Init();

        ModSystemUpdates.Wire(first);
        ModSystemUpdates.Wire(second);

        Assert.Single(((IDependencyContainer)first).ResolveAll<ModSystemBase>());
        Assert.Single(((IDependencyContainer)second).ResolveAll<ModSystemBase>());
    }
}
