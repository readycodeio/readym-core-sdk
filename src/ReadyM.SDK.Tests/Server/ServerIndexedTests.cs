using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Indexed shapes on the server, where the index cannot live in the world the relay keeps.
/// </summary>
/// <remarks>
/// A component a mod owns is stored in a heap the relay hands out but does not read, and the index
/// Friflo would keep for it does nothing. So the index sits on this side, and every write of an
/// indexed value goes through the accessor that maintains it.
/// </remarks>
public class ServerIndexedTests : ServerSdkTest
{
    [Fact]
    public void A_shape_is_found_by_the_value_it_is_indexed_on()
    {
        var passenger = Spawn<Passenger>();

        passenger.Ticket = 99;

        Assert.True(Entities.TryLookup<Ticketed, int>(99, out var found));
        Assert.Equal(99, found.Ticket);
        Assert.Equal(IdentityOf(passenger).Id, EntityHandle.Of(found).Id);
    }

    [Fact]
    public void A_value_nothing_holds_is_not_found()
    {
        Spawn<Passenger>().Ticket = 1;

        Assert.False(Entities.TryLookup<Ticketed, int>(2, out _));
    }

    // -- agreeing with a query whatever the index saw --------------------------------------------

    /// <summary>Writes the value the way a delta from a client lands, without the accessor seeing it.</summary>
    private void WritePastTheIndex(Passenger passenger, int ticket)
    {
        var raw = IdentityOf(passenger);
        var component = Relay.Get<TicketedComponent>(raw);

        component.ticket = ticket;
        Relay.Set(raw, component);
    }

    [Fact]
    public void A_value_written_past_the_index_is_still_found()
    {
        var passenger = Spawn<Passenger>();

        WritePastTheIndex(passenger, 77);

        Assert.True(Entities.TryLookup<Ticketed, int>(77, out var found));
        Assert.Equal(IdentityOf(passenger).Id, EntityHandle.Of(found).Id);
    }

    [Fact]
    public void A_key_moved_past_the_index_no_longer_finds_the_entity()
    {
        var passenger = Spawn<Passenger>();

        passenger.Ticket = 13;
        WritePastTheIndex(passenger, 14);

        Assert.False(Entities.TryLookup<Ticketed, int>(13, out _));
        Assert.True(Entities.TryLookup<Ticketed, int>(14, out _));
    }

    [Fact]
    public void A_deleted_entity_is_not_found()
    {
        var passenger = Spawn<Passenger>();

        passenger.Ticket = 12;
        Entities.Delete(passenger);

        Assert.False(Entities.TryLookup<Ticketed, int>(12, out _));
    }

    /// <summary>Found by walking once, then answered from the index like any other value.</summary>
    [Fact]
    public void A_value_found_by_walking_is_remembered()
    {
        var passenger = Spawn<Passenger>();

        WritePastTheIndex(passenger, 31);
        Entities.TryLookup<Ticketed, int>(31, out _);
        Relay.ResetCounters();

        Assert.True(Entities.TryLookup<Ticketed, int>(31, out _));
        Assert.Equal(0, Relay.FindByIndexCalls);
    }

    [Fact]
    public void Moving_the_value_moves_the_entity_in_the_index()
    {
        var passenger = Spawn<Passenger>();

        passenger.Ticket = 4;
        passenger.Ticket = 5;

        Assert.False(Entities.TryLookup<Ticketed, int>(4, out _));
        Assert.True(Entities.TryLookup<Ticketed, int>(5, out _));
    }

    [Fact]
    public void Writing_another_value_leaves_the_index_alone()
    {
        var passenger = Spawn<Passenger>();

        passenger.Ticket = 6;
        passenger.Fare = 20;

        Assert.True(Entities.TryLookup<Ticketed, int>(6, out var found));
        Assert.Equal(20, found.Fare);
    }

    /// <summary>Two entities on different values, so one cannot evict the other.</summary>
    [Fact]
    public void Two_entities_keep_their_own_entries()
    {
        var first = Spawn<Passenger>();
        var second = Spawn<Passenger>();

        first.Ticket = 10;
        second.Ticket = 20;

        Assert.True(Entities.TryLookup<Ticketed, int>(10, out var a));
        Assert.True(Entities.TryLookup<Ticketed, int>(20, out var b));
        Assert.Equal(IdentityOf(first).Id, EntityHandle.Of(a).Id);
        Assert.Equal(IdentityOf(second).Id, EntityHandle.Of(b).Id);
    }

    /// <summary>An indexed value still reads from a chunk, which is where a query gets it.</summary>
    [Fact]
    public void An_indexed_value_reads_from_a_chunk()
    {
        for (var i = 1; i <= 3; i++)
            Spawn<Passenger>().Ticket = i;

        Relay.ResetCounters();

        var total = 0;

        foreach (var passenger in Entities.Query<Passenger>())
            total += passenger.Ticket;

        Assert.Equal(6, total);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }
}
