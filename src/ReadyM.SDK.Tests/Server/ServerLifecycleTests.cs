using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Creating and deleting entities through the SDK rather than by arranging the relay directly.
/// </summary>
/// <remarks>
/// Server-only entities: never replicated. The networked variants take a scope and an owner, which
/// the spec still leaves open, so they are not here.
/// </remarks>
public class ServerLifecycleTests : ServerSdkTest
{
    [Fact]
    public void A_created_entity_is_alive_and_carries_the_shape()
    {
        var npc = Entities.Create<Npc>();

        Assert.True(npc.IsValid);
        Assert.True(npc.Is<Npc>());
        Assert.Equal(0, npc.Hp);
    }

    [Fact]
    public void A_created_entity_is_visited_by_a_query()
    {
        Entities.Create<Npc>();
        Entities.Create<Npc>();
        Entities.Create<Boulder>();

        var count = 0;

        foreach (var _ in Entities.Query<Npc>())
            count++;

        Assert.Equal(2, count);
    }

    [Fact]
    public void Values_written_after_creation_read_back()
    {
        var npc = Entities.Create<Npc>();

        npc.Hp = 5;
        npc.X = 6f;
        npc.Label = "seven";

        Assert.Equal(5, npc.Hp);
        Assert.Equal(6f, npc.X);
        Assert.Equal("seven", npc.Label);
    }

    [Fact]
    public void A_deleted_entity_is_gone()
    {
        var npc = Entities.Create<Npc>();

        Assert.True(Entities.Delete(npc));
        Assert.False(npc.IsValid);
        Assert.False(Entities.Delete(npc));
    }

    [Fact]
    public void A_deleted_entity_is_no_longer_visited()
    {
        var doomed = Entities.Create<Npc>();

        Entities.Create<Npc>();
        Entities.Delete(doomed);

        var count = 0;

        foreach (var _ in Entities.Query<Npc>())
            count++;

        Assert.Equal(1, count);
    }

    /// <summary>
    /// Deleting is a structural change, so it belongs on the identity path. The survivors have to
    /// keep their values, which is what catches a botched swap-removal.
    /// </summary>
    [Fact]
    public void Deleting_inside_an_identity_loop_leaves_the_survivors_intact()
    {
        for (var i = 0; i < 10; i++)
            Entities.Create<Npc>().Hp = i;

        var entities = Entities.Query<Npc>().Identities();
        var doomed = new List<Npc>();

        while (entities.MoveNext())
        {
            var npc = entities.Current;

            if (npc.Hp % 2 == 0)
                doomed.Add(npc);
        }

        entities.Dispose();

        foreach (var npc in doomed)
            Assert.True(Entities.Delete(npc));

        var survivors = new List<int>();

        foreach (var npc in Entities.Query<Npc>())
            survivors.Add(npc.Hp);

        survivors.Sort();
        Assert.Equal([1, 3, 5, 7, 9], survivors);
    }

    [Fact]
    public void The_relay_is_asked_once_per_create_and_delete()
    {
        Relay.ResetCounters();

        var npc = Entities.Create<Npc>();

        Entities.Delete(npc);

        Assert.Equal(1, Relay.CreateCalls);
        Assert.Equal(1, Relay.DeleteCalls);
    }
}
