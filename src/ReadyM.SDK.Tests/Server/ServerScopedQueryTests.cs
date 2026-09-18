using ReadyM.SDK.Exceptions;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Queries narrowed to a scope, answered by the relay from the links pointing at it.
/// </summary>
/// <remarks>
/// The cost follows what the scope holds rather than what the world does, which is why the relay
/// walks links instead of scanning archetypes. The result is a list of entities, so the loop walks
/// identities: a scope's entities are scattered through their archetypes and form no chunks.
/// </remarks>
public class ServerScopedQueryTests : ServerSdkTest
{
    private TestArea Area(int id)
    {
        var area = Spawn<TestArea>();

        area.AreaId = id;

        return area;
    }

    private T Spawn<T>(TestArea area) where T : struct, IArchetype => Entities.Create<T>(area);

    [Fact]
    public void A_scoped_query_visits_only_what_the_scope_holds()
    {
        var north = Area(1);
        var south = Area(2);

        Spawn<Guard>(north).Route = 10;
        Spawn<Guard>(north).Route = 20;
        Spawn<Guard>(south).Route = 30;

        var routes = 0;

        foreach (var guard in Entities.Query<Guard>().InScope(north))
            routes += guard.Route;

        Assert.Equal(30, routes);
    }

    [Fact]
    public void An_entity_in_no_scope_is_not_visited()
    {
        var north = Area(1);

        Spawn<Guard>(north).Route = 5;
        Spawn<Guard>().Route = 99;

        var routes = 0;

        foreach (var guard in Entities.Query<Guard>().InScope(north))
            routes += guard.Route;

        Assert.Equal(5, routes);
    }

    [Fact]
    public void A_shape_the_query_did_not_ask_for_is_not_visited()
    {
        var north = Area(1);

        Spawn<Guard>(north);
        Spawn<Npc>(north);

        var count = 0;

        foreach (var _ in Entities.Query<Guard>().InScope(north))
            count++;

        Assert.Equal(1, count);
    }

    /// <summary>One crossing for the whole loop, the same as an unscoped query.</summary>
    [Fact]
    public void A_scoped_query_crosses_the_boundary_once()
    {
        var north = Area(1);

        for (var i = 0; i < 4; i++)
            Spawn<Guard>(north);

        Relay.ResetCounters();

        var count = 0;

        foreach (var _ in Entities.Query<Guard>().InScope(north))
            count++;

        Assert.Equal(4, count);
        Assert.Equal(1, Relay.QueryInScopeCalls);
        Assert.Equal(0, Relay.QueryCalls);
    }

    [Fact]
    public void An_empty_scope_visits_nothing()
    {
        var north = Area(1);

        Spawn<Guard>(Area(2));

        var count = 0;

        foreach (var _ in Entities.Query<Guard>().InScope(north))
            count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public void A_write_through_a_scoped_query_reaches_the_relay()
    {
        var north = Area(1);
        var guard = Spawn<Guard>(north);

        foreach (var found in Entities.Query<Guard>().InScope(north))
            found.Route = 7;

        Assert.Equal(7, Relay.Get<PatrolComponent>(IdentityOf(guard)).route);
    }

    /// The flow a mod writes: find the scope by its index, then narrow to it.
    [Fact]
    public void A_scope_found_by_its_index_can_be_queried()
    {
        var north = Area(42);

        Spawn<Guard>(north).Route = 8;
        Spawn<Guard>(Area(43)).Route = 9;

        Assert.True(Entities.TryLookup<TestArea, int>(42, out var found));

        var routes = 0;

        foreach (var guard in Entities.Query<Guard>().InScope(found))
            routes += guard.Route;

        Assert.Equal(8, routes);
    }

    /// Deleting inside a scoped loop is held, the same as in any other query.
    [Fact]
    public void A_delete_inside_a_scoped_query_is_held()
    {
        var north = Area(1);

        Spawn<Guard>(north);
        Spawn<Guard>(north);
        Relay.ResetCounters();

        foreach (var guard in Entities.Query<Guard>().InScope(north))
        {
            Entities.Delete(guard);

            Assert.Equal(0, Relay.DeleteCalls);
        }

        Assert.Equal(2, Relay.DeleteCalls);
    }

    // -- creating inside a scope -------------------------------------------------------------------

    [Fact]
    public void An_entity_created_in_a_scope_is_held_by_it()
    {
        var north = Area(1);
        Relay.ResetCounters();

        Entities.Create<Guard>(north);

        Assert.Equal(1, Relay.CreateInScopeCalls);
        Assert.Equal(1, CountIn(north));
    }

    [Fact]
    public void An_entity_created_without_a_scope_is_held_by_none()
    {
        var north = Area(1);

        Spawn<Guard>();

        Assert.Equal(0, CountIn(north));
    }

    [Fact]
    public void Two_scopes_hold_their_own_entities()
    {
        var north = Area(1);
        var south = Area(2);

        Entities.Create<Guard>(north);
        Entities.Create<Guard>(south);
        Entities.Create<Guard>(south);

        Assert.Equal(1, CountIn(north));
        Assert.Equal(2, CountIn(south));
    }

    /// A scope that is gone cannot hold anything.
    [Fact]
    public void Creating_in_a_scope_that_is_gone_is_refused()
    {
        var north = Area(1);

        Entities.Delete(north);

        Assert.Throws<InvalidEntityException>(() => Entities.Create<Guard>(north));
    }

    /// Creating is refused inside a query, scope or no scope.
    [Fact]
    public void Creating_in_a_scope_inside_a_query_is_refused()
    {
        var north = Area(1);

        Spawn<Guard>(north);

        // Written the way READYM002 forbids, deliberately: the run time has to refuse it too.
#pragma warning disable READYM002
        Assert.Throws<StructuralChangeInQueryException>(() =>
        {
            foreach (var _ in Entities.Query<Guard>())
                Entities.Create<Guard>(north);
        });
#pragma warning restore READYM002
    }

    private int CountIn(TestArea area)
    {
        var count = 0;

        foreach (var _ in Entities.Query<Guard>().InScope(area))
            count++;

        return count;
    }

    // -- every width a query goes to ---------------------------------------------------------------

    [Fact]
    public void Two_shapes_can_be_scoped()
    {
        var north = Area(1);

        Spawn<Guard>(north).Route = 3;
        Spawn<Guard>(Area(2)).Route = 9;

        var routes = 0;
        var visited = 0;

        foreach (var (position, patrol) in Entities.Query<Position, Patrol>().InScope(north))
        {
            routes += patrol.Route;
            _ = position.X;
            visited++;
        }

        Assert.Equal(1, visited);
        Assert.Equal(3, routes);
    }

    /// The widest a query goes, narrowed to one scope.
    [Fact]
    public void Six_shapes_can_be_scoped()
    {
        var north = Area(1);
        var adventurer = Spawn<Adventurer>(north);

        adventurer.X = 1f;
        adventurer.Hp = 2;
        adventurer.Label = "three";
        adventurer.Pace = 4f;
        adventurer.Gold = 5;
        adventurer.Spirit = 6f;

        Spawn<Adventurer>(Area(2));

        var visited = 0;

        foreach (var (position, vitals, named, speed, wealth, mood)
                 in Entities.Query<Position, Vitals, Named, Speed, Wealth, Mood>().InScope(north))
        {
            Assert.Equal(1f, position.X);
            Assert.Equal(2, vitals.Hp);
            Assert.Equal("three", named.Label);
            Assert.Equal(4f, speed.Pace);
            Assert.Equal(5, wealth.Gold);
            Assert.Equal(6f, mood.Spirit);
            visited++;
        }

        Assert.Equal(1, visited);
    }

    /// An entity in the scope missing one of the shapes is not visited.
    [Fact]
    public void A_scoped_multi_shape_query_still_needs_every_shape()
    {
        var north = Area(1);

        Spawn<Guard>(north);
        Spawn<Npc>(north);

        var count = 0;

        foreach (var (_, _) in Entities.Query<Position, Patrol>().InScope(north))
            count++;

        Assert.Equal(1, count);
    }

    [Fact]
    public void A_default_multi_shape_scoped_query_visits_nothing()
    {
        var count = 0;

        foreach (var (_, _) in Entities.Query<Position, Patrol>().InScope(default))
            count++;

        Assert.Equal(0, count);
    }

    /// Deleting inside a scoped loop over several shapes is held until it ends, the same as any
    /// other query, which is what says the loop opened a query scope and closed it again.
    [Fact]
    public void A_delete_inside_a_scoped_multi_shape_query_is_held()
    {
        var north = Area(1);

        Spawn<Guard>(north);
        Spawn<Guard>(north);
        Relay.ResetCounters();

        var visited = 0;

        foreach (var (position, _) in Entities.Query<Position, Patrol>().InScope(north))
        {
            Assert.True(Entities.Delete(position));

            Assert.Equal(0, Relay.DeleteCalls);

            visited++;
        }

        Assert.Equal(2, visited);
        Assert.Equal(2, Relay.DeleteCalls);
    }
}
