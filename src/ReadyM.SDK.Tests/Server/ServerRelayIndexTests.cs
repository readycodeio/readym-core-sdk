using ReadyM.SDK.Entity;
using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Indexed components the relay owns, where the index cannot live on this side.
/// </summary>
/// <remarks>
/// The relay stores these and keeps their index, but only notices a change when handed the whole
/// value: an index is rebuilt from the old value and the new one together, so writing through the
/// address a slot carries cannot be seen afterwards. Both the write and the lookup therefore cross
/// the boundary, which is what separates this from a component a mod owns.
/// </remarks>
public class ServerRelayIndexTests : ServerSdkTest
{
    [Fact]
    public void A_write_to_an_indexed_value_crosses_the_boundary()
    {
        var replicated = Spawn<Replicated>();
        Relay.ResetCounters();

        replicated.NetId = 7;

        Assert.Equal(1, Relay.SetComponentCalls);
        Assert.Equal(7, Relay.Get<RelayNetIdComponent>(IdentityOf(replicated)).NetId);
    }

    /// A value the relay does not index is written the cheap way, straight through the slot.
    [Fact]
    public void A_write_to_anything_else_does_not()
    {
        var replicated = Spawn<Replicated>();
        Relay.ResetCounters();

        replicated.X = 3f;

        Assert.Equal(0, Relay.SetComponentCalls);
    }

    [Fact]
    public void A_shape_is_found_by_the_value_the_relay_indexes()
    {
        var replicated = Spawn<Replicated>();

        replicated.NetId = 42;

        Assert.True(Entities.TryLookup<Networked2, int>(42, out var found));
        Assert.Equal(42, found.NetId);
        Assert.Equal(IdentityOf(replicated).Id, EntityHandle.Of(found).Id);
    }

    [Fact]
    public void The_lookup_crosses_the_boundary()
    {
        Spawn<Replicated>().NetId = 5;
        Relay.ResetCounters();

        Entities.TryLookup<Networked2, int>(5, out _);

        Assert.Equal(1, Relay.FindByIndexCalls);
    }

    [Fact]
    public void A_value_nothing_holds_is_not_found()
    {
        Spawn<Replicated>().NetId = 1;

        Assert.False(Entities.TryLookup<Networked2, int>(2, out _));
    }

    [Fact]
    public void Moving_the_value_moves_the_entity_in_the_relay_index()
    {
        var replicated = Spawn<Replicated>();

        replicated.NetId = 8;
        replicated.NetId = 9;

        Assert.False(Entities.TryLookup<Networked2, int>(8, out _));
        Assert.True(Entities.TryLookup<Networked2, int>(9, out _));
    }

    /// Writing the unindexed half of the same component keeps the entry where it was.
    [Fact]
    public void Writing_another_field_of_the_same_component_keeps_the_entry()
    {
        var replicated = Spawn<Replicated>();

        replicated.NetId = 11;
        replicated.Owner = 3;

        Assert.True(Entities.TryLookup<Networked2, int>(11, out var found));
        Assert.Equal(3, found.Owner);
    }

    [Fact]
    public void Two_entities_keep_their_own_entries()
    {
        var first = Spawn<Replicated>();
        var second = Spawn<Replicated>();

        first.NetId = 100;
        second.NetId = 200;

        Assert.True(Entities.TryLookup<Networked2, int>(100, out var a));
        Assert.True(Entities.TryLookup<Networked2, int>(200, out var b));
        Assert.Equal(IdentityOf(first).Id, EntityHandle.Of(a).Id);
        Assert.Equal(IdentityOf(second).Id, EntityHandle.Of(b).Id);
    }

    /// A relay-indexed value still reads from a chunk, so a query over it costs nothing extra.
    [Fact]
    public void An_indexed_value_reads_from_a_chunk()
    {
        for (var i = 1; i <= 3; i++)
            Spawn<Replicated>().NetId = i;

        Relay.ResetCounters();

        var total = 0;

        foreach (var replicated in Entities.Query<Replicated>())
            total += replicated.NetId;

        Assert.Equal(6, total);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }
}
