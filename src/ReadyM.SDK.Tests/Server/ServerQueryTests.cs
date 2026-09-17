using ReadyM.SDK.Exceptions;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

public class ServerQueryTests : ServerSdkTest
{
    [Fact]
    public void A_query_visits_every_entity_of_the_archetype()
    {
        Spawn<Npc>();
        Spawn<Npc>();
        Spawn<Npc>();

        Assert.Equal(3, CountIdentities(Entities.Query<Npc>()));
    }

    [Fact]
    public void A_query_skips_an_entity_that_does_not_carry_the_whole_set()
    {
        Spawn<Npc>();
        Spawn<Boulder>();

        Assert.Equal(1, CountIdentities(Entities.Query<Npc>()));
        Assert.Equal(1, CountIdentities(Entities.Query<Boulder>()));
    }

    /// A mixin's query is over its own component, so it matches every archetype that includes it.
    [Fact]
    public void A_mixin_query_reaches_every_archetype_that_includes_it()
    {
        Spawn<Npc>();
        Spawn<Boulder>();

        Assert.Equal(2, CountIdentities(Entities.Query<Position>()));
        Assert.Equal(1, CountIdentities(Entities.Query<Vitals>()));
    }

    /// PositionComponent lives in a pinned heap the AOT side owns, so its slot carries an address.
    [Fact]
    public void A_value_the_relay_holds_by_address_is_read_through_the_archetype()
    {
        var entity = Spawn<Npc>();
        Relay.Set(IdentityOf(entity), new PositionComponent { x = 3f, y = 4f });

        Assert.Equal(3f, entity.X);
        Assert.Equal(4f, entity.Y);
    }

    /// VitalsComponent lives in a heap the mod owns, so its slot carries a heap handle and an index.
    [Fact]
    public void A_value_the_relay_holds_by_heap_handle_is_read_through_the_archetype()
    {
        var entity = Spawn<Npc>();
        Relay.Set(IdentityOf(entity), new VitalsComponent { hp = 7, maxHp = 30 });

        Assert.Equal(7, entity.Hp);
        Assert.Equal(30, entity.MaxHp);
    }

    [Fact]
    public void A_write_through_the_archetype_reaches_both_kinds_of_heap()
    {
        var entity = Spawn<Npc>();

        entity.X = 11f;
        entity.Hp = 12;

        Assert.Equal(11f, Relay.Get<PositionComponent>(IdentityOf(entity)).x);
        Assert.Equal(12, Relay.Get<VitalsComponent>(IdentityOf(entity)).hp);
    }

    [Fact]
    public void A_write_inside_a_query_loop_reaches_the_relay()
    {
        Spawn<Npc>();
        Spawn<Npc>();

        foreach (var npc in Entities.Query<Npc>())
            npc.Hp = 99;

        foreach (var npc in Entities.Query<Npc>())
            Assert.Equal(99, npc.Hp);
    }

    [Fact]
    public void Running_a_query_crosses_the_boundary_once()
    {
        Spawn<Npc>();
        Spawn<Npc>();
        Spawn<Npc>();
        Relay.ResetCounters();

        CountIdentities(Entities.Query<Npc>());

        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    /// <summary>
    /// What the buffered path costs on the server: the loop itself is one crossing, but every value
    /// a body reads is another. This is the number the chunk path collapses to zero.
    /// </summary>
    /// <remarks>
    /// Walked through Identities on purpose. A plain foreach over a named shape now takes the chunk
    /// path, which is what <c>ServerChunkQueryTests</c> asserts instead.
    /// </remarks>
    [Fact]
    public void Reading_in_the_loop_body_crosses_the_boundary_once_per_read()
    {
        for (var i = 0; i < 10; i++)
            Spawn<Npc>();

        Relay.ResetCounters();

        var entities = Entities.Query<Npc>().Identities();

        while (entities.MoveNext())
        {
            var npc = entities.Current;
            _ = npc.X + npc.Y + npc.Hp;
        }

        entities.Dispose();

        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(30, Relay.SlotCalls);
    }

    /// <summary>
    /// Creating is refused rather than held. Growing an archetype replaces the array a chunk names,
    /// so the loop would carry on writing to storage nothing reads any more.
    /// </summary>
    [Fact]
    public void Creating_an_entity_inside_a_query_is_refused()
    {
        Spawn<Npc>();

        // Written the way READYM002 forbids, deliberately: the analyzer stops this at the call
        // site, and this checks the run time refuses it too, which is what covers a call the
        // analyzer cannot see through.
#pragma warning disable READYM002
        Assert.Throws<StructuralChangeInQueryException>(() =>
        {
            foreach (var _ in Entities.Query<Npc>())
                Entities.Create<Npc>();
        });
#pragma warning restore READYM002
    }

    /// <summary>The way to do it: the loop collects, and the creating happens once it is over.</summary>
    [Fact]
    public void Creating_after_the_loop_is_how_a_query_spawns_entities()
    {
        for (var i = 0; i < 5; i++)
            Spawn<Npc>().Hp = i;

        var npcTotal = 0;
        var wanted = new List<int>();

        foreach (var npc in Entities.Query<Npc>())
        {
            wanted.Add(npc.Hp + 10);
            npcTotal += npc.Hp;
        }

        Assert.Equal(10, npcTotal);

        foreach (var hp in wanted)
            Entities.Create<Npc>().Hp = hp;

        var newTotal = 0;

        foreach (var npc in Entities.Query<Npc>())
            newTotal += npc.Hp;

        Assert.Equal(10 + 10 + 50, newTotal);
    }

    [Fact]
    public void Deleting_entities_during_query_applies_afterwards()
    {
        Relay.ResetCounters();
        
        for (var i = 0; i < 5; i++)
        {
            var npc = Spawn<Npc>();
            npc.Hp = i;
        }
        
        foreach (var npc in Entities.Query<Npc>())
        {
            // delete ones with even HP
            if (npc.Hp % 2 == 0)
            {
                Entities.Delete(npc);
            }
        }

        Assert.Equal(3, Relay.DeleteCalls);

        var total = 0;
        foreach (var npc in Entities.Query<Npc>())
        {
            total += npc.Hp;
        }

        Assert.Equal(4, total); // 1 + 3
    }
}