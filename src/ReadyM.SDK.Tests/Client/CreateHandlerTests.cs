using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// What a shape asked to run as one of its entities is created, which is where a default belongs.
public class CreateHandlerTests : ClientSdkTest
{
    [Fact]
    public void A_shape_sets_its_own_default_as_the_entity_appears()
        => Assert.Equal(7, Entities.Create<Parcel>().Mark);

    /// The archetype's own handler runs as well as the one on what it includes.
    [Fact]
    public void An_archetype_runs_its_own_as_well_as_what_it_includes()
    {
        var crate = Entities.Create<Crate>();

        Assert.Equal(7, crate.Mark);
        Assert.Equal(1, crate.Seal);
    }

    /// Every shape on the entity gets its own handler run, not just the first one found.
    [Fact]
    public void Every_included_shape_runs_its_own()
    {
        var convoy = Entities.Create<Convoy>();

        Assert.Equal(7, convoy.Mark);
        Assert.Equal(1, convoy.Heard);
        Assert.Equal(3, convoy.Tally);
        Assert.Equal(4, convoy.Sealed);
    }

    /// A handler asks for what it needs by naming it, and the game's services answer.
    [Fact]
    public void A_handler_is_given_what_it_asked_the_game_for()
        => Assert.Equal(1, Entities.Create<Herald>().Heard);

    /// Nothing runs for a shape that asked for nothing, which is almost every shape.
    [Fact]
    public void A_shape_that_asked_for_nothing_is_left_alone()
        => Assert.Equal(0, Entities.Create<Chest>().Gold);
}
