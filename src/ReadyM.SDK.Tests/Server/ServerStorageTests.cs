using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// The two kinds of component storage the ABI allows, and that an ordinary archetype spans both.
/// </summary>
/// <remarks>
/// A component the AOT side owns is blittable, pinned, and reached by address. One a mod owns is
/// reached only through its heap handle and an index, because it may hold managed references and so
/// can never be pinned. Slot resolution branches on that, and only a fixture that covers both keeps
/// the branch honest.
/// </remarks>
public class ServerStorageTests : ServerSdkTest
{
    [Fact]
    public void One_archetype_spans_both_kinds_of_heap()
    {
        var npc = IdentityOf(Spawn<Npc>());

        Assert.False(Relay.IsManaged<PositionComponent>(npc));
        Assert.True(Relay.IsManaged<VitalsComponent>(npc));
    }

    [Fact]
    public void A_component_holding_a_managed_reference_round_trips()
    {
        var npc = Spawn<Npc>();

        npc.Label = "Guard Captain";

        Assert.Equal("Guard Captain", npc.Label);
        Assert.Equal("Guard Captain", Relay.Get<NamedComponent>(IdentityOf(npc)).label);
    }

    [Fact]
    public void A_managed_reference_survives_a_collection_that_may_relocate_the_heap()
    {
        var npc = Spawn<Npc>();
        npc.Label = string.Join('-', Enumerable.Range(0, 64));

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.Equal(string.Join('-', Enumerable.Range(0, 64)), npc.Label);
    }

    [Fact]
    public void Every_entity_in_a_query_keeps_its_own_managed_value()
    {
        for (var i = 0; i < 8; i++)
            Spawn<Npc>().Label = $"npc {i}";

        var labels = new List<string>();

        foreach (var npc in Entities.Query<Npc>())
            labels.Add(npc.Label);

        Assert.Equal([.. Enumerable.Range(0, 8).Select(i => $"npc {i}")], labels);
    }
}
