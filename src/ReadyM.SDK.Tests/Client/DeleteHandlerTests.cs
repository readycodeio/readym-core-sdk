using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// What a shape and a service asked to run as an entity goes. Unlike a create handler, these run
/// wherever the entity is, so a client lets go of what it hung off a replicated entity too.
public class DeleteHandlerTests : ClientSdkTest
{
    [Fact]
    public void A_shape_is_told_before_one_of_its_entities_goes()
    {
        Entities.Delete(Entities.Create<Crateload>());

        Assert.Contains("shape", Container.Resolve<Ledger>().Order);
    }

    /// The entity is still whole while a handler runs, which is the only reason one is worth having.
    [Fact]
    public void A_handler_can_still_read_the_entity()
    {
        Entities.Delete(Entities.Create<Crateload>());

        Assert.Equal([3, 3], Container.Resolve<Ledger>().Chalk);
    }

    [Fact]
    public void A_service_is_told_before_a_shape_it_watches_goes()
    {
        Entities.Delete(Entities.Create<Crateload>());

        Assert.Contains("service", Container.Resolve<Ledger>().Order);
    }

    /// The mirror of creation, where the shape's own handler goes first: what a shape set up for
    /// itself is still there while whatever was built on top is let go.
    [Fact]
    public void A_watcher_goes_first_and_the_shape_last()
    {
        Entities.Delete(Entities.Create<Crateload>());

        Assert.Equal(["service", "shape"], Container.Resolve<Ledger>().Order);
    }

    /// Nothing else is touched. A handler is keyed on the shape, not on anything being deleted.
    [Fact]
    public void A_shape_that_was_not_deleted_hears_nothing()
    {
        Entities.Create<Crateload>();
        Entities.Delete(Entities.Create<Parcel>());

        Assert.Empty(Container.Resolve<Ledger>().Order);
    }

    /// <summary>
    /// Nobody asked the SDK for this one. It stands in for an entity the game or a snapshot takes
    /// away, which is the case a client has to survive: the handler hangs off the store, not off the
    /// delete the SDK offers.
    /// </summary>
    [Fact]
    public void An_entity_taken_away_from_under_the_sdk_runs_them_too()
    {
        var crate = Entities.Create<Crateload>();

        Store.GetEntityByRawEntity(EntityHandle.Of(crate).RawEntity).DeleteEntity();

        Assert.Equal(["service", "shape"], Container.Resolve<Ledger>().Order);
    }

    /// <summary>
    /// An archetype that holds nothing of its own has no component and no marker to be keyed by, and
    /// is still the name people write. It is watched by everything it is made of, the way a query
    /// matches it.
    /// </summary>
    [Fact]
    public void A_shape_that_holds_nothing_of_its_own_is_watched_all_the_same()
    {
        var turnstile = Container.Resolve<Turnstile>();
        var ticketed = Entities.Create<Ticketed>();

        Assert.Equal(1, turnstile.Inside);

        Entities.Delete(ticketed);

        Assert.Equal(0, turnstile.Inside);
    }

    /// Deleting inside a query is held until the loop ends, and the handlers run when it does.
    [Fact]
    public void One_deleted_inside_a_query_runs_them_when_the_loop_ends()
    {
        Entities.Create<Crateload>();

        foreach (var crate in Entities.Query<Crateload>())
            Entities.Delete(crate);

        Assert.Equal(["service", "shape"], Container.Resolve<Ledger>().Order);
    }
}
