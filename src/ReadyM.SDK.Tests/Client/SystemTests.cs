using Friflo.Engine.ECS;
using ReadyM.Api.DI;
using Friflo.Engine.ECS.Systems;
using ReadyM.SDK.Client.Systems;
using ReadyM.SDK.Systems;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// What [System] makes of a class: one instance the container hands out by name, and an update a
/// game can call without knowing what it is called.
public class SystemTests : ClientSdkTest
{
    [Fact]
    public void A_system_is_one_instance_anything_can_ask_for()
    {
        SystemRegistry.RegisterAll(Container);

        Assert.Same(Container.Resolve<Counting>(), Container.Resolve<Counting>());
    }

    /// A game updates what it is handed rather than naming any of them.
    [Fact]
    public void A_game_updates_what_it_was_handed()
    {
        SystemRegistry.RegisterAll(Container);

        foreach (var system in SystemRegistry.Resolve(Container))
            system.Update(new Tick(0.5f, 10f));

        Assert.Equal(1, Container.Resolve<Counting>().Ticks);
        Assert.Equal(0.5f, Container.Resolve<Regeneration>().Healed);
    }

    /// The whole client path: the game's loop updates the group, the group updates the systems, and
    /// the tick it puts together is the one a system is handed.
    [Fact]
    public void A_games_loop_updates_every_declared_system()
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
        Assert.Equal(10.25f, recording.Last.Time);

        // Zero on the first update, so "every thirtieth" runs on the first and then every thirtieth.
        Assert.Equal(1ul, recording.Last.Count);
    }

    /// Nothing runs while the world is absent, and a skipped update is not counted: the spacing is
    /// in updates a system saw.
    [Fact]
    public void Nothing_runs_or_counts_while_the_world_is_absent()
    {
        ModSystems.Refresh(Entities);

        var root = new SystemRoot(Store);

        root.Add(new ModSystemUpdates(Container));
        root.Update(new UpdateTick(0.5f, 10f));

        SystemRegistry.RegisterAll(Container);

        Assert.Equal(0, Container.Resolve<Counting>().Ticks);
    }

    /// A container stops taking registrations once the game is running, so updating must only ever
    /// resolve. Registering is the loading window's business, and saying it again on every tick
    /// brought the server's polling thread down.
    [Fact]
    public void Updating_never_registers_anything()
    {
        SystemRegistry.RegisterAll(Container);

        var closed = new Closed(Container);

        foreach (var system in SystemRegistry.Resolve(closed))
            system.Update(new Tick(0.5f, 10f));

        Assert.Equal(1, Container.Resolve<Counting>().Ticks);
    }

    /// The tick reaches the handler that asked for it, and the one that did not still runs.
    [Fact]
    public void The_tick_reaches_a_handler_that_asked_for_it()
    {
        SystemRegistry.RegisterAll(Container);

        var regeneration = Container.Resolve<Regeneration>();

        ((IModSystem)regeneration).Update(new Tick(0.25f, 1f));
        ((IModSystem)regeneration).Update(new Tick(0.25f, 2f));

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
