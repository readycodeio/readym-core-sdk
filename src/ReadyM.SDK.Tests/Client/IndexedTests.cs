using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// Shapes found by a value rather than by walking, and the writes that keep that possible.
public class IndexedTests : ClientSdkTest
{
    // -- indexed by a property the SDK declares ----------------------------------------------------

    [Fact]
    public void A_shape_is_found_by_the_value_it_is_indexed_on()
    {
        var ticketed = Entities.Create<Ticketed>();

        ticketed.Ticket = 42;

        Assert.True(Entities.TryLookup<Registered, int>(42, out var found));
        Assert.Equal(42, found.Ticket);
        Assert.Equal(EntityHandle.Of(ticketed).Id, EntityHandle.Of(found).Id);
    }

    [Fact]
    public void A_value_nothing_holds_is_not_found()
    {
        Entities.Create<Ticketed>().Ticket = 1;

        Assert.False(Entities.TryLookup<Registered, int>(2, out _));
    }

    /// <summary>
    /// The write that used to be the hazard: assigning through a reference would leave the index
    /// pointing at the old value, so an indexed setter assigns the whole component instead.
    /// </summary>
    [Fact]
    public void Moving_the_value_moves_the_entity_in_the_index()
    {
        var ticketed = Entities.Create<Ticketed>();

        ticketed.Ticket = 7;
        ticketed.Ticket = 8;

        Assert.False(Entities.TryLookup<Registered, int>(7, out _));
        Assert.True(Entities.TryLookup<Registered, int>(8, out var found));
        Assert.Equal(8, found.Ticket);
    }

    /// The same hazard through a value token, which is the only way a client can write at all.
    [Fact]
    public void Overriding_the_value_moves_the_entity_in_the_index()
    {
        var docked = Entities.Create<Docked>();

        Assert.True(docked.Override(Berth.Field.Slot, 7));
        Assert.True(docked.Override(Berth.Field.Slot, 8));

        Assert.False(Entities.TryLookup<Berth, int>(7, out _));
        Assert.True(Entities.TryLookup<Berth, int>(8, out var found));
        Assert.Equal(8, found.Slot);
    }

    /// Overriding anything else on an indexed component must leave the index where it was.
    [Fact]
    public void Overriding_another_value_leaves_the_index_alone()
    {
        var docked = Entities.Create<Docked>();

        Assert.True(docked.Override(Berth.Field.Slot, 5));
        Assert.True(docked.Override(Berth.Field.Deck, 2));

        Assert.True(Entities.TryLookup<Berth, int>(5, out var found));
        Assert.Equal(2, found.Deck);
    }

    /// Writing a value that is not the index must not disturb it.
    [Fact]
    public void Writing_another_value_leaves_the_index_alone()
    {
        var ticketed = Entities.Create<Ticketed>();

        ticketed.Ticket = 5;
        ticketed.Note = "held";

        Assert.True(Entities.TryLookup<Registered, int>(5, out var found));
        Assert.Equal("held", found.Note);
    }

    [Fact]
    public void An_archetype_may_be_indexed_by_its_own_property()
    {
        var station = Entities.Create<Station>();

        station.Code = 314;

        Assert.True(Entities.TryLookup<Station, int>(314, out var found));
        Assert.Equal(314, found.Code);
    }

    // -- indexed because the borrowed component already is -----------------------------------------

    [Fact]
    public void A_shape_over_an_indexed_component_is_found_by_its_value()
    {
        var ticketed = Entities.Create<Ticketed>();

        ticketed.Tag = "north";

        Assert.True(Entities.TryLookup<Tagged, string>("north", out var found));
        Assert.Equal("north", found.Tag);
    }

    [Fact]
    public void Moving_a_borrowed_indexed_value_moves_the_entity_too()
    {
        var ticketed = Entities.Create<Ticketed>();

        ticketed.Tag = "north";
        ticketed.Tag = "south";

        Assert.False(Entities.TryLookup<Tagged, string>("north", out _));
        Assert.True(Entities.TryLookup<Tagged, string>("south", out _));
    }

    /// Two shapes on one entity, each indexed on its own component.
    [Fact]
    public void Two_indexes_on_one_entity_stay_independent()
    {
        var ticketed = Entities.Create<Ticketed>();

        ticketed.Ticket = 11;
        ticketed.Tag = "east";

        Assert.True(Entities.TryLookup<Registered, int>(11, out _));
        Assert.True(Entities.TryLookup<Tagged, string>("east", out _));
    }

    // -- what the shapes declare -------------------------------------------------------------------

    [Fact]
    public void An_indexed_shape_says_what_it_is_indexed_by()
    {
        Assert.True(typeof(IIndexed<int>).IsAssignableFrom(typeof(Registered)));
        Assert.True(typeof(IIndexed<string>).IsAssignableFrom(typeof(Tagged)));
        Assert.False(typeof(IIndexed<int>).IsAssignableFrom(typeof(Tagged)));
    }

    /// <summary>
    /// A chunk write moves nothing in an index, so an indexed value is read-only from a view and
    /// written through the handle instead.
    /// </summary>
    [Fact]
    public void An_indexed_value_is_read_only_from_a_chunk()
    {
        Entities.Create<Ticketed>().Ticket = 3;

        foreach (var ticketed in Entities.Query<Ticketed>())
        {
            Assert.Equal(3, ticketed.Ticket);

            ticketed.Handle.TryAs<Registered>(out var registered);
            registered.Ticket = 4;
        }

        Assert.True(Entities.TryLookup<Registered, int>(4, out _));
    }
}
