using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.RPC;

namespace ReadyM.SDK.Tests.Rpc;

/// What the generated half of an RPC class declares, and what a host does with it: one instance in
/// DI that anything can ask for, built from its constructor the way a [Service] is.
public class RpcHandlerRegistryTests
{
    [Fact]
    public void A_declared_class_is_one_instance_anything_can_ask_for()
    {
        using var container = Registered();

        Assert.Same(container.Resolve<Handlers>(), container.Resolve<Handlers>());
    }

    /// Which is what lets an RPC class ask for what it needs rather than reaching for a static.
    [Fact]
    public void A_declared_class_is_handed_what_it_asked_for()
    {
        using var container = Registered();

        Assert.NotNull(container.Resolve<Handlers>().Logger);
    }

    /// <summary>
    /// A module initializer runs the registration where the target has one, the mod loader runs it
    /// for every assembly it loads, and a host may say it again once the last mod is in.
    /// </summary>
    [Fact]
    public void Registering_everything_twice_still_leaves_one_of_each()
    {
        using var container = Registered();

        RpcHandlerRegistry.RegisterAll(container, RpcSide.Server);

        Assert.Same(container.Resolve<Handlers>(), container.Resolve<Handlers>());
    }

    /// A mod that registered its RPC class by hand before the SDK did it keeps working.
    [Fact]
    public void A_class_registered_by_hand_as_well_still_resolves()
    {
        using var container = Registered();

        container.RegisterSingleton<Handlers>();

        Assert.NotNull(container.Resolve<Handlers>());
    }

    /// <summary>A client class is never put in a server container, or the other way about.</summary>
    /// <remarks>
    /// One process may run both, as the relay's own tests do. A client class started in a server
    /// container has no relay client to register its handlers with, and throws as it starts.
    /// </remarks>
    [Fact]
    public void A_class_declared_for_one_side_is_left_out_of_the_others_container()
    {
        using var container = Registered();

        Assert.Throws<DryIoc.ContainerException>(() => container.Resolve<OtherSide>());
    }

    private static Container Registered()
    {
        var container = new Container();

        container.Init();
        container.RegisterSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        container.RegisterSingleton<ILogger>(NullLogger.Instance);

        RpcHandlerRegistry.Declare<Handlers>(RpcSide.Server);
        RpcHandlerRegistry.Declare<OtherSide>(RpcSide.Client);
        RpcHandlerRegistry.RegisterAll(container, RpcSide.Server);

        return container;
    }

    private sealed class Container : DependencyContainerBase;

    /// Stands in for the half of an RPC class a mod writes. The generated half is what declares it,
    /// which a generator test covers; this is about what the declaration then does.
    private sealed class Handlers(ILogger logger)
    {
        public ILogger Logger { get; } = logger;
    }

    /// Declared for the side the test does not register, so it must not turn up.
    private sealed class OtherSide;
}
