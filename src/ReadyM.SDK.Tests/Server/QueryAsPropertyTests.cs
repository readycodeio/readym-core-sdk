using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Handing a query out of an api, rather than a sequence of what it matched.
/// </summary>
/// <remarks>
/// A query is a description, so passing one on costs nothing and the loop that finally walks it
/// still binds the chunk path. Materialising the matches cannot: the fast path hands out a view
/// bound to chunk memory, and that cannot leave the loop.
/// </remarks>
public class QueryAsPropertyTests : ServerSdkTest
{
    /// What a game api would expose in place of IEnumerable.
    private EntityQuery<Npc> AllNpcs => Entities.Query<Npc>();

    [Fact]
    public void A_query_handed_out_by_a_property_still_walks_chunks()
    {
        for (var i = 0; i < 4; i++)
            Spawn<Npc>().Hp = i;

        Relay.ResetCounters();

        var total = 0;

        foreach (var npc in AllNpcs)
            total += npc.Hp;

        Assert.Equal(6, total);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    [Fact]
    public void A_write_through_one_reaches_the_relay()
    {
        var npc = Spawn<Npc>();

        foreach (var found in AllNpcs)
            found.Hp = 12;

        Assert.Equal(12, Relay.Get<VitalsComponent>(IdentityOf(npc)).hp);
    }
}
