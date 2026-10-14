using ReadyM.SDK.Services;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A [Service] is registered by its own type, so a host that wants every service playing a role asks the
/// registry rather than the container.
public class ServiceResolveAllTests : ClientSdkTest
{
    [Fact]
    public void Resolve_all_gives_the_services_playing_a_role_in_type_name_order()
    {
        var greeters = ServiceRegistry.ResolveAll<IGreeter>(Container);

        Assert.Equal(new[] { "alpha", "zulu" }, greeters.Select(greeter => greeter.Greet()));
    }

    /// The same instances the container hands out by name, not copies.
    [Fact]
    public void Resolve_all_gives_the_one_instance_of_each()
    {
        var greeters = ServiceRegistry.ResolveAll<IGreeter>(Container);

        Assert.Same(Container.Resolve<AlphaGreeter>(), greeters[0]);
    }

    /// Every other fixture service is declared too, and none of them comes back.
    [Fact]
    public void Resolve_all_skips_services_that_do_not_play_the_role()
    {
        Assert.Equal(2, ServiceRegistry.ResolveAll<IGreeter>(Container).Count);
        Assert.Empty(ServiceRegistry.ResolveAll<IUnplayedRole>(Container));
    }

    private interface IUnplayedRole;
}
