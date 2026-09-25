using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.SDK.Services;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A [Service] that declared a Start or a Stop is a hosted service, so the game starts it with
/// everything else it hosts and stops it when the container goes.
public class ServiceLifetimeTests
{
    [Fact]
    public void Start_runs_when_the_game_starts_its_hosted_services()
    {
        using var container = Started();

        Assert.Equal(1, container.Resolve<Bookends>().Started);
        Assert.True(container.Resolve<StartOnly>().Started);
    }

    /// Nothing has stopped yet, which is the half of the pair a test can easily get wrong.
    [Fact]
    public void Stop_does_not_run_on_the_way_up()
    {
        using var container = Started();

        Assert.Equal(0, container.Resolve<Bookends>().Stopped);
    }

    [Fact]
    public void Stop_runs_when_the_container_goes()
    {
        var container = Started();
        var bookends = container.Resolve<Bookends>();

        container.Dispose();

        Assert.Equal(1, bookends.Stopped);
    }

    /// A service with neither end is not hosted, so starting the game does not build it.
    [Fact]
    public void A_service_with_neither_end_is_left_alone_until_something_asks_for_it()
    {
        using var container = Started();

        Assert.Equal(3, container.Resolve<Settings>().Difficulty);
    }

    private static Container Started()
    {
        var container = new Container();

        container.Init();
        container.RegisterSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        container.RegisterSingleton<ILogger>(NullLogger.Instance);

        ServiceRegistry.RegisterAll(container);

        container.StartHostedServices();

        return container;
    }

    private sealed class Container : DependencyContainerBase;
}
