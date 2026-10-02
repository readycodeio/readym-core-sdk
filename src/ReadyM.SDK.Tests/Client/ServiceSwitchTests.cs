using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.SDK.Services;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// Switching a service off and on again while the game runs, which a mod does through IServices and
/// a service sees through its own OnEnabled and OnDisabled.
public class ServiceSwitchTests
{
    [Fact]
    public void A_service_starts_enabled()
    {
        using var container = Started();

        Assert.True(container.Resolve<Lanterns>().Enabled);
        Assert.True(container.Resolve<IServices>().IsEnabled<Lanterns>());
    }

    [Fact]
    public void A_disabled_service_stops_updating()
    {
        using var container = Started();
        var patrol = container.Resolve<Lanterns>();

        Tick<Lanterns>(container);
        container.Resolve<IServices>().Disable<Lanterns>();
        Tick<Lanterns>(container);

        Assert.Equal(1, patrol.Ticks);
        Assert.False(patrol.Enabled);
    }

    [Fact]
    public void Enabling_it_again_puts_it_back_in_the_loop()
    {
        using var container = Started();
        var services = container.Resolve<IServices>();
        var patrol = container.Resolve<Lanterns>();

        services.Disable<Lanterns>();
        Tick<Lanterns>(container);
        services.Enable<Lanterns>();
        Tick<Lanterns>(container);

        Assert.Equal(1, patrol.Ticks);
    }

    /// The clock stops with it, so a service reads when it last ran rather than how long it was off.
    [Fact]
    public void A_disabled_service_is_not_handed_the_clock()
    {
        using var container = Started();
        var recording = container.Resolve<Recording>();

        Tick<Recording>(container, elapsed: 1f);
        container.Resolve<IServices>().Disable<Recording>();
        Tick<Recording>(container, elapsed: 99f);

        Assert.Equal(1f, recording.Last.Elapsed);
    }

    [Fact]
    public void OnDisabled_runs_as_it_leaves_the_loop()
    {
        using var container = Started();
        var patrol = container.Resolve<Lanterns>();

        container.Resolve<IServices>().Disable<Lanterns>();

        Assert.Equal(1, patrol.Disables);
    }

    /// The first one is the game starting it, which is why it is already one before anything asks.
    [Fact]
    public void OnEnabled_runs_once_at_startup_and_again_on_every_enable()
    {
        using var container = Started();
        var services = container.Resolve<IServices>();
        var patrol = container.Resolve<Lanterns>();

        Assert.Equal(1, patrol.Enables);

        services.Disable<Lanterns>();
        services.Enable<Lanterns>();

        Assert.Equal(2, patrol.Enables);
    }

    /// Set up first, told it is about to run second. A service reads its own state in OnEnabled.
    [Fact]
    public void Start_runs_before_the_first_OnEnabled()
    {
        using var container = Started();

        Assert.Equal(["Start", "OnEnabled"], container.Resolve<Opening>().Order);
    }

    /// Declaring OnEnabled is enough to be hosted, or the first one would never be reached.
    [Fact]
    public void A_service_with_only_OnEnabled_is_still_started_by_the_game()
    {
        using var container = Started();

        Assert.Equal(1, container.Resolve<Lanterns>().Enables);
    }

    [Fact]
    public void Disabling_it_twice_warns_and_leaves_it_alone()
    {
        var warnings = new Warnings();
        using var container = Started(warnings);
        var patrol = container.Resolve<Lanterns>();

        container.Resolve<IServices>().Disable<Lanterns>();
        container.Resolve<IServices>().Disable<Lanterns>();

        Assert.Equal(1, patrol.Disables);
        Assert.Single(warnings.Logged);
        Assert.Contains("Lanterns", warnings.Logged[0]);
    }

    [Fact]
    public void Enabling_one_that_is_already_enabled_warns_and_leaves_it_alone()
    {
        var warnings = new Warnings();
        using var container = Started(warnings);
        var patrol = container.Resolve<Lanterns>();

        container.Resolve<IServices>().Enable<Lanterns>();

        Assert.Equal(1, patrol.Enables);
        Assert.Single(warnings.Logged);
    }

    /// Which is the only way to do it: Enabled has no setter, so nothing can move the switch while
    /// leaving OnDisabled unrun.
    [Fact]
    public void A_service_switches_itself_off_through_the_same_door()
    {
        using var container = Started();
        var curfew = container.Resolve<Curfew>();

        Tick<Curfew>(container);
        Tick<Curfew>(container);

        Assert.Equal(1, curfew.Ticks);
        Assert.Equal(1, curfew.Disables);
    }

    /// A service with nothing to update still carries the switch, since holding one instance of
    /// something is reason enough to declare it and reason enough to switch it off.
    [Fact]
    public void A_service_with_nothing_to_update_still_carries_the_switch()
    {
        using var container = Started();

        container.Resolve<IServices>().Disable<Settings>();

        Assert.False(container.Resolve<Settings>().Enabled);
    }

    /// Disabling one says nothing about the next, which is what makes this usable across mods.
    [Fact]
    public void Switching_one_off_leaves_the_others_running()
    {
        using var container = Started();

        container.Resolve<IServices>().Disable<Lanterns>();
        Tick<Lanterns>(container);
        Tick<Counting>(container);

        Assert.Equal(0, container.Resolve<Lanterns>().Ticks);
        Assert.Equal(1, container.Resolve<Counting>().Ticks);
    }

    /// One service at a time, since this container holds only what the switch needs. The skip is
    /// the service's own, so a game's loop reaches it the same way.
    private static void Tick<TService>(IDependencyContainer container, float elapsed = 0f)
        where TService : class, IUpdatingService
        => container.Resolve<TService>().Update(new UpdateTime(0.5f, elapsed));

    private static Container Started(ILogger? logger = null)
    {
        var container = new Container();

        container.Init();
        container.RegisterSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        container.RegisterSingleton(logger ?? NullLogger.Instance);

        ServiceRegistry.RegisterAll(container);

        container.StartHostedServices();
        ServiceRegistry.StartAll(container);

        return container;
    }

    private sealed class Container : DependencyContainerBase;

    /// Keeps what was warned about, since a double switch is reported rather than thrown.
    private sealed class Warnings : ILogger
    {
        public List<string> Logged { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Logged.Add(formatter(state, exception));
        }
    }
}
