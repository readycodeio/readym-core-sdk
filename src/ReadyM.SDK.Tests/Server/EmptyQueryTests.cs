using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// A query nobody gave a world to, which matches nothing rather than faulting.
/// </summary>
/// <remarks>
/// What this is for: an api hands out a query instead of a sequence, so the loop that walks it keeps
/// the chunk path. A property that sometimes has nothing to look in needs a value meaning exactly
/// that, and default is it.
/// </remarks>
public class EmptyQueryTests : ServerSdkTest
{
    private static EntityQuery<Npc> Nothing => default;

    private static EntityQuery<Position, Vitals> NothingMulti => default;

    private static ScopedQuery<Guard> NoScope => default;

    private static ScopedQuery<Position, Vitals> NoScopeMulti => default;

    [Fact]
    public void A_default_query_visits_nothing()
    {
        Spawn<Npc>();

        var count = 0;

        foreach (var _ in Nothing)
            count++;

        Assert.Equal(0, count);
    }

    /// The chunk path is what a foreach over a shape with a view binds, so it needs the same.
    [Fact]
    public void A_default_query_crosses_nothing()
    {
        Spawn<Npc>();
        Relay.ResetCounters();

        foreach (var _ in Nothing) { }

        Assert.Equal(0, Relay.QueryCalls);
    }

    [Fact]
    public void A_default_query_walked_by_identity_visits_nothing()
    {
        Spawn<Npc>();

        Assert.Equal(0, CountIdentities(Nothing));
    }

    [Fact]
    public void A_default_multi_shape_query_visits_nothing()
    {
        Spawn<Npc>();

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

    /// A real query narrowed to a scope that was never found matches nothing, which is the case a
    /// property hits when there is no current area.
    [Fact]
    public void A_query_scoped_to_nothing_visits_nothing()
    {
        var area = Spawn<TestArea>();

        Entities.Create<Guard>(area);

        var count = 0;

        foreach (var _ in Entities.Query<Guard>().InScope(default))
            count++;

        Assert.Equal(0, count);
    }

    /// <summary>Walking an empty one must not leave a query scope open behind it.</summary>
    [Fact]
    public void An_empty_query_leaves_no_scope_open()
    {
        foreach (var _ in Nothing) { }
        foreach (var _ in NoScope) { }

        var npc = Spawn<Npc>();

        Assert.True(Entities.Delete(npc));
        Assert.Equal(1, Relay.DeleteCalls);
    }

    [Fact]
    public void A_default_multi_shape_scoped_query_visits_nothing()
    {
        Spawn<Npc>();

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

        Relay.ResetCounters();

        var npc = Spawn<Npc>();

        Assert.True(Entities.Delete(npc));
        Assert.Equal(1, Relay.DeleteCalls);
    }

    /// The other way a scoped query ends up with nothing to look in: a real scope, but a query that
    /// was never given a world.
    [Fact]
    public void A_default_query_narrowed_to_a_real_scope_visits_nothing()
    {
        var area = Spawn<TestArea>();

        Entities.Create<Guard>(area);

        var count = 0;

        foreach (var _ in default(EntityQuery<Guard>).InScope(area))
            count++;

        foreach (var (_, _) in default(EntityQuery<Position, Vitals>).InScope(area))
            count++;

        Assert.Equal(0, count);
    }
}
