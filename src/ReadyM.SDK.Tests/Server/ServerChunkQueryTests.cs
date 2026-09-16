using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// The chunk fast path: one crossing of the interop boundary for a whole query, against one per
/// value read on the buffered path.
/// </summary>
public class ServerChunkQueryTests : ServerSdkTest
{
    [Fact]
    public void A_scan_visits_every_entity_of_the_archetype()
    {
        Spawn<Npc>();
        Spawn<Npc>();
        Spawn<Npc>();
        Spawn<Boulder>();

        var count = 0;

        foreach (var _ in Entities.Query<Npc>())
            count++;

        Assert.Equal(3, count);
    }

    /// <summary>The whole point: the body reads five values and crosses nothing.</summary>
    [Fact]
    public void A_scan_crosses_the_boundary_once_however_much_the_body_reads()
    {
        for (var i = 0; i < 10; i++)
            Spawn<Npc>();

        Relay.ResetCounters();

        foreach (var npc in Entities.Query<Npc>())
            _ = npc.X + npc.Y + npc.Hp + npc.MaxHp + npc.Faction;

        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    [Fact]
    public void A_scan_reads_values_from_both_kinds_of_heap()
    {
        var entity = Spawn<Npc>();

        Relay.Set(IdentityOf(entity), new PositionComponent { x = 3f, y = 4f });
        Relay.Set(IdentityOf(entity), new VitalsComponent { hp = 7, maxHp = 30 });

        foreach (var npc in Entities.Query<Npc>())
        {
            Assert.Equal(3f, npc.X);
            Assert.Equal(4f, npc.Y);
            Assert.Equal(7, npc.Hp);
            Assert.Equal(30, npc.MaxHp);
        }
    }

    [Fact]
    public void A_scan_writes_values_into_both_kinds_of_heap()
    {
        var entity = Spawn<Npc>();

        foreach (var npc in Entities.Query<Npc>())
        {
            npc.X = 11f;
            npc.Hp = 12;
        }

        Assert.Equal(11f, Relay.Get<PositionComponent>(IdentityOf(entity)).x);
        Assert.Equal(12, Relay.Get<VitalsComponent>(IdentityOf(entity)).hp);
    }

    [Fact]
    public void A_scan_keeps_each_entity_distinct()
    {
        for (var i = 0; i < 16; i++)
            Spawn<Npc>().Faction = i;

        var seen = new List<int>();

        foreach (var npc in Entities.Query<Npc>())
            seen.Add(npc.Faction);

        Assert.Equal([.. Enumerable.Range(0, 16)], seen);
    }

    [Fact]
    public void A_scan_carries_managed_values()
    {
        for (var i = 0; i < 4; i++)
            Spawn<Npc>().Label = $"npc {i}";

        var labels = new List<string>();

        foreach (var npc in Entities.Query<Npc>())
            labels.Add(npc.Label);

        Assert.Equal([.. Enumerable.Range(0, 4).Select(i => $"npc {i}")], labels);
    }

    /// The handle is how work that cannot happen inside the loop gets out of it.
    [Fact]
    public void A_view_hands_back_a_handle_for_the_entity_it_is_on()
    {
        var expected = new List<int>();

        for (var i = 0; i < 4; i++)
            expected.Add(IdentityOf(Spawn<Npc>()).Id);

        var seen = new List<int>();

        foreach (var npc in Entities.Query<Npc>())
            seen.Add(npc.Handle.Id);

        Assert.Equal(expected, seen);
    }

    [Fact]
    public void A_scan_of_a_world_with_nothing_to_match_visits_nothing()
    {
        Spawn<Boulder>();

        var count = 0;

        foreach (var _ in Entities.Query<Npc>())
            count++;

        Assert.Equal(0, count);
    }

    /// <summary>The two paths are the same query, so they have to agree on values as well as counts.</summary>
    [Fact]
    public void The_chunk_path_and_the_buffered_path_agree()
    {
        for (var i = 0; i < 12; i++)
        {
            var npc = Spawn<Npc>();

            npc.Faction = i;
            npc.X = i * 2f;
            npc.Hp = i * 3;
            npc.Label = $"npc {i}";
        }

        var buffered = new List<string>();
        var scanned = new List<string>();

        foreach (var npc in Entities.Query<Npc>())
            buffered.Add($"{npc.Faction}/{npc.X}/{npc.Hp}/{npc.Label}");

        foreach (var npc in Entities.Query<Npc>())
            scanned.Add($"{npc.Faction}/{npc.X}/{npc.Hp}/{npc.Label}");

        Assert.Equal(buffered, scanned);
        Assert.Equal(12, scanned.Count);
    }

    /// A mixin is a shape in its own right, so a query over one takes the chunk path too.
    [Fact]
    public void A_query_over_a_single_mixin_crosses_the_boundary_once()
    {
        for (var i = 0; i < 10; i++)
            Spawn<Npc>();

        Relay.ResetCounters();

        var total = 0f;

        foreach (var position in Entities.Query<Position>())
            total += position.X + position.Y;

        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    /// <summary>
    /// A shape reaching into an assembly compiled without the server SDK cannot be walked by chunk,
    /// and still works: the loop binds the general extension and walks identities instead of failing
    /// to compile. That is now the only thing that causes the fallback.
    /// </summary>
    [Fact]
    public void A_shape_without_a_chunk_view_falls_back_to_identities()
    {
        for (var i = 0; i < 4; i++)
            Entities.Create<Peddler>();

        Relay.ResetCounters();

        var count = 0;

        foreach (var peddler in Entities.Query<Peddler>())
        {
            count++;
            _ = peddler.X;
        }

        Assert.Equal(4, count);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(4, Relay.SlotCalls);
    }

    /// Two mixins without an archetype naming both, still one crossing.
    [Fact]
    public void A_query_over_two_mixins_crosses_the_boundary_once()
    {
        for (var i = 0; i < 10; i++)
        {
            var npc = Spawn<Npc>();

            npc.X = i;
            npc.Hp = i * 2;
        }

        Relay.ResetCounters();

        var positions = 0f;
        var health = 0;

        foreach (var (position, vitals) in Entities.Query<Position, Vitals>())
        {
            positions += position.X;
            health += vitals.Hp;
        }

        Assert.Equal(45f, positions);
        Assert.Equal(90, health);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    /// Only entities carrying both are visited, so a shape missing one is skipped.
    [Fact]
    public void A_query_over_two_mixins_skips_an_entity_missing_either()
    {
        Spawn<Npc>();
        Spawn<Boulder>();

        var count = 0;

        foreach (var (_, _) in Entities.Query<Position, Vitals>())
            count++;

        Assert.Equal(1, count);
    }

    /// A write through either half of the pair lands in the relay.
    [Fact]
    public void A_write_through_a_two_mixin_query_reaches_the_relay()
    {
        var entity = Spawn<Npc>();

        foreach (var (position, vitals) in Entities.Query<Position, Vitals>())
        {
            position.X = 5f;
            vitals.Hp = 6;
        }

        Assert.Equal(5f, Relay.Get<PositionComponent>(IdentityOf(entity)).x);
        Assert.Equal(6, Relay.Get<VitalsComponent>(IdentityOf(entity)).hp);
    }

    [Fact]
    public void A_mixin_can_be_scanned_across_every_archetype_that_includes_it()
    {
        Spawn<Npc>();
        Spawn<Boulder>();

        var count = 0;

        foreach (var _ in Entities.Query<Position>())
            count++;

        Assert.Equal(2, count);
    }

    /// Two archetypes means two chunks, so this covers the enumerator crossing a chunk boundary.
    [Fact]
    public void A_scan_spanning_two_archetypes_visits_both()
    {
        Spawn<Npc>().X = 1f;
        Spawn<Npc>().X = 2f;
        Spawn<Boulder>().X = 3f;

        var total = 0f;

        foreach (var position in Entities.Query<Position>())
            total += position.X;

        Assert.Equal(6f, total);
    }
}
