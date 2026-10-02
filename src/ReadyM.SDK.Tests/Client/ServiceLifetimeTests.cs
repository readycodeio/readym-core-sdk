using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.SDK.Services;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A [Service] is started when the host says its game is up, which is its own moment and later
/// than the container being filled. A Stop still rides the container going away.
public class ServiceLifetimeTests
{
    [Fact]
    public void Start_runs_when_the_game_says_it_is_ready()
    {
        using var container = Started();

        Assert.Equal(1, container.Resolve<Bookends>().Started);
        Assert.True(container.Resolve<StartOnly>().Started);
    }

    /// <summary>Filling a container is not a game being ready, and only the game decides that.</summary>
    /// <remarks>
    /// A host builds its container long before its game is up. Started with it, a service reaching
    /// for the game in its Start finds nothing bound, which is what this separation is for.
    /// </remarks>
    [Fact]
    public void Nothing_is_started_by_the_container_alone()
    {
        using var container = Filled();

        container.StartHostedServices();

        Assert.Equal(0, container.Resolve<Bookends>().Started);
    }

    /// A host says it once, but says it on a container, and a process may hold more than one.
    [Fact]
    public void Saying_it_twice_starts_each_service_once()
    {
        using var container = Started();

        ServiceRegistry.StartAll(container);

        Assert.Equal(1, container.Resolve<Bookends>().Started);
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
        var container = Filled();

        container.StartHostedServices();
        ServiceRegistry.StartAll(container);

        return container;
    }

    private static Container Filled()
    {
        var container = new Container();

        container.Init();
        container.RegisterSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        container.RegisterSingleton<ILogger>(NullLogger.Instance);

        ServiceRegistry.RegisterAll(container);

        return container;
    }

    private sealed class Container : DependencyContainerBase;
}
