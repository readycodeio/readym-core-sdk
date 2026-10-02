using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A query nobody gave a world to, which matches nothing rather than faulting.
public class EmptyQueryTests : ClientSdkTest
{
    private static EntityQuery<Monster> Nothing => default;

    private static EntityQuery<Health, Placement> NothingMulti => default;

    private static ScopedQuery<Guard> NoScope => default;

    private static ScopedQuery<Health, Placement> NoScopeMulti => default;

    [Fact]
    public void A_default_query_visits_nothing()
    {
        SpawnMonster();

        var count = 0;

        foreach (var _ in Nothing)
            count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public void A_default_query_walked_by_identity_visits_nothing()
    {
        SpawnMonster();

        var entities = Nothing.Identities();
        var count = 0;

        while (entities.MoveNext())
            count++;

        entities.Dispose();

        Assert.Equal(0, count);
    }

    [Fact]
    public void A_default_multi_shape_query_visits_nothing()
    {
        SpawnMonster();

        var count = 0;

        foreach (var (_, _) in NothingMulti)
            count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public void A_default_scoped_query_visits_nothing()
    {
        var count = 0;

        foreach (var _ in NoScope)
            count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public void A_query_scoped_to_nothing_visits_nothing()
    {
        var area = Entities.Create<TestArea>();

        Entities.Create<Guard>(area);

        var count = 0;

        foreach (var _ in Entities.Query<Guard>().InScope(default))
            count++;

        Assert.Equal(0, count);
    }

    /// Walking an empty one must not leave a query scope open behind it.
    [Fact]
    public void An_empty_query_leaves_no_scope_open()
    {
        foreach (var _ in Nothing) { }
        foreach (var _ in NoScope) { }

        var monster = SpawnMonster();

        Assert.True(Entities.Delete(monster));
        Assert.False(monster.IsValid);
    }

    [Fact]
    public void A_default_multi_shape_scoped_query_visits_nothing()
    {
        SpawnMonster();

        var count = 0;

        foreach (var (_, _) in NoScopeMulti)
            count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public void An_empty_multi_shape_scoped_query_leaves_no_scope_open()
    {
        foreach (var (_, _) in NothingMulti) { }
        foreach (var (_, _) in NoScopeMulti) { }

        var monster = SpawnMonster();

        Assert.True(Entities.Delete(monster));
        Assert.False(monster.IsValid);
    }

    /// The other way a scoped query ends up with nothing to look in: a real scope, but a query that
    /// was never given a world.
    [Fact]
    public void A_default_query_narrowed_to_a_real_scope_visits_nothing()
    {
        var area = Entities.Create<TestArea>();

        Entities.Create<Guard>(area);

        var count = 0;

        foreach (var _ in default(EntityQuery<Guard>).InScope(area))
            count++;

        foreach (var (_, _) in default(EntityQuery<Health, Placement>).InScope(area))
            count++;

        Assert.Equal(0, count);
    }
}
