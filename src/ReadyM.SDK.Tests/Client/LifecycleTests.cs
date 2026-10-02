using ReadyM.SDK.Exceptions;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// Creating and deleting entities through the SDK rather than through the store.
public class LifecycleTests : ClientSdkTest
{
    [Fact]
    public void A_created_entity_is_alive_and_carries_the_shape()
    {
        var monster = Entities.Create<Monster>();

        Assert.True(monster.IsValid);
        Assert.True(monster.Is<Monster>());
        Assert.Equal(0f, monster.Hp);
    }

    [Fact]
    public void A_created_entity_is_visited_by_a_query()
    {
        Entities.Create<Monster>();
        Entities.Create<Monster>();
        Entities.Create<Prop>();

        Assert.Equal(2, Count(Entities.Query<Monster>()));
    }

    [Fact]
    public void A_deleted_entity_is_gone()
    {
        var monster = Entities.Create<Monster>();

        Assert.True(Entities.Delete(monster));
        Assert.False(monster.IsValid);
        Assert.False(Entities.Delete(monster));
    }

    [Fact]
    public void A_deleted_entity_is_no_longer_visited()
    {
        var doomed = Entities.Create<Monster>();

        Entities.Create<Monster>();
        Entities.Delete(doomed);

        Assert.Equal(1, Count(Entities.Query<Monster>()));
    }

    /// The buffered loop is what makes this safe: the visited set was taken before the body ran.
    [Fact]
    public void Deleting_inside_a_query_loop_is_allowed()
    {
        for (var i = 0; i < 6; i++)
            Entities.Create<Monster>().Hp = i;

        foreach (var monster in Entities.Query<Monster>())
            if (monster.Hp % 2 == 0)
                Entities.Delete(monster);

        var survivors = new List<float>();

        foreach (var monster in Entities.Query<Monster>())
            survivors.Add(monster.Hp);

        survivors.Sort();
        Assert.Equal([1f, 3f, 5f], survivors);
    }

    /// <summary>
    /// The client refuses it for the same reason the server does: a loop holds the ids the store had
    /// when it started, and creating moves entities between archetypes under it.
    /// </summary>
    [Fact]
    public void Creating_an_entity_inside_a_loop_is_refused()
    {
        Entities.Create<Monster>();

        // Written the way READYM002 forbids, deliberately: the analyzer stops this at the call
        // site, and this checks the run time refuses it too, which is what covers a call the
        // analyzer cannot see through.
#pragma warning disable READYM002
        Assert.Throws<StructuralChangeInQueryException>(() =>
        {
            foreach (var _ in Entities.Query<Monster>())
                Entities.Create<Monster>();
        });
#pragma warning restore READYM002
    }

    [Fact]
    public void Creating_after_the_loop_works()
    {
        Entities.Create<Monster>();

        var visited = 0;

        foreach (var _ in Entities.Query<Monster>())
            visited++;

        Entities.Create<Monster>();

        Assert.Equal(1, visited);
        Assert.Equal(2, Count(Entities.Query<Monster>()));
    }
}
