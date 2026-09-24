using Friflo.Engine.ECS;
using ReadyM.Api.DI;
using Friflo.Engine.ECS.Systems;
using ReadyM.SDK.Client.Systems;
using ReadyM.SDK.Services;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// What [Service] makes of a class: one instance the container hands out by name, and an update a
/// game can call without knowing what it is called.
public class ServiceTests : ClientSdkTest
{
    [Fact]
    public void A_service_is_one_instance_anything_can_ask_for()
    {
        ServiceRegistry.RegisterAll(Container);

        Assert.Same(Container.Resolve<Counting>(), Container.Resolve<Counting>());
    }

    /// Which is what lets one service be handed another, rather than reaching for a static.
    [Fact]
    public void A_service_is_handed_the_services_it_asked_for()
    {
        ServiceRegistry.RegisterAll(Container);

        var counting = Container.Resolve<Counting>();
        var auditing = Container.Resolve<Auditing>();

        ((IUpdatingService)counting).Update(new UpdateTime(0.5f, 10f));
        ((IUpdatingService)auditing).Update(new UpdateTime(0.5f, 10f));

        Assert.Equal(1, auditing.Seen);
    }

    /// A service with nothing to run is still registered: holding one instance of something is
    /// reason enough to declare it.
    [Fact]
    public void A_service_with_nothing_to_run_is_still_one_instance()
    {
        ServiceRegistry.RegisterAll(Container);

        Container.Resolve<Settings>().Difficulty = 5;

        Assert.Equal(5, Container.Resolve<Settings>().Difficulty);
    }

    /// A game updates what it is handed rather than naming any of them.
    [Fact]
    public void A_game_updates_what_it_was_handed()
    {
        ServiceRegistry.RegisterAll(Container);

        foreach (var service in ServiceRegistry.Resolve(Container))
            service.Update(new UpdateTime(0.5f, 10f));

        Assert.Equal(1, Container.Resolve<Counting>().Ticks);
        Assert.Equal(0.5f, Container.Resolve<Regeneration>().Healed);
    }

    /// And what it is handed is only the services that declared an update.
    [Fact]
    public void A_game_is_not_handed_a_service_with_nothing_to_update()
    {
        ServiceRegistry.RegisterAll(Container);

        Assert.DoesNotContain(ServiceRegistry.Resolve(Container), service => service is Settings);
    }

    /// The whole client path: the game's loop updates the group, the group updates the services, and
    /// the clock it puts together is the one a service reads.
    [Fact]
    public void A_games_loop_updates_every_declared_service()
    {
        Entities.Create<Core.World>();
        ModSystems.Refresh(Entities);

        var root = new SystemRoot(Store);

        root.Add(new ModSystemUpdates(Container));

        root.Update(new UpdateTick(0.5f, 10f));
        root.Update(new UpdateTick(0.25f, 10.25f));

        var recording = Container.Resolve<Recording>();

        Assert.Equal(2, Container.Resolve<Counting>().Ticks);
        Assert.Equal(0.25f, recording.Last.DeltaTime);
        Assert.Equal(10.25f, recording.Last.Elapsed);

        // Zero on the first update, so "every thirtieth" runs on the first and then every thirtieth.
        Assert.Equal(1ul, recording.Last.Ticks);
    }

    /// Nothing runs while the world is absent, and a skipped update is not counted: the spacing is
    /// in updates a service saw.
    [Fact]
    public void Nothing_runs_or_counts_while_the_world_is_absent()
    {
        ModSystems.Refresh(Entities);

        var root = new SystemRoot(Store);

        root.Add(new ModSystemUpdates(Container));
        root.Update(new UpdateTick(0.5f, 10f));

        ServiceRegistry.RegisterAll(Container);

        Assert.Equal(0, Container.Resolve<Counting>().Ticks);
    }

    /// A container stops taking registrations once the game is running, so updating must only ever
    /// resolve. Registering is the loading window's business, and saying it again on every tick
    /// brought the server's polling thread down.
    [Fact]
    public void Updating_never_registers_anything()
    {
        ServiceRegistry.RegisterAll(Container);

        var closed = new Closed(Container);

        foreach (var service in ServiceRegistry.Resolve(closed))
            service.Update(new UpdateTime(0.5f, 10f));

        Assert.Equal(1, Container.Resolve<Counting>().Ticks);
    }

    /// An update reads the clock off the service rather than being handed it.
    [Fact]
    public void The_clock_a_service_reads_is_the_one_it_was_updated_with()
    {
        ServiceRegistry.RegisterAll(Container);

        var regeneration = Container.Resolve<Regeneration>();

        ((IUpdatingService)regeneration).Update(new UpdateTime(0.25f, 1f));
        ((IUpdatingService)regeneration).Update(new UpdateTime(0.25f, 2f));

        Assert.Equal(0.5f, regeneration.Healed);
    }

    /// Answers what it is asked for and refuses to be added to, the way a container does once the
    /// game is running.
    private sealed class Closed(IDependencyContainer inner) : IDependencyContainer
    {
        public T Resolve<T>() => inner.Resolve<T>();

        public IEnumerable<T> ResolveAll<T>() => inner.ResolveAll<T>();

        public void RegisterSingleton<TService>(bool replace = false) => throw Refused();

        public void RegisterSingleton<TService>(TService instance, bool replace = false) => throw Refused();

        public void RegisterSingleton<TService>(Type implementationType, bool replace = false) => throw Refused();

        public void RegisterSingleton<TService, TImplementation>(bool replace = false)
            where TImplementation : TService
            => throw Refused();

        public void RegisterSingleton<TService, TImplementation>(TImplementation instance, bool replace = false)
            where TImplementation : TService
            => throw Refused();

        private static InvalidOperationException Refused()
            => new("Container does not allow further registrations.");
    }
}
