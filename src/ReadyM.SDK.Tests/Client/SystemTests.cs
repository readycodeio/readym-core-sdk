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
}
