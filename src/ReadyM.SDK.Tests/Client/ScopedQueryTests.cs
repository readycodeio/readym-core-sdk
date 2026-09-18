using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;
using FrifloEntity = Friflo.Engine.ECS.Entity;

namespace ReadyM.SDK.Tests.Client;

/// Queries narrowed to one scope, answered from the links pointing at it.
public class ScopedQueryTests : ClientSdkTest
{
    private FrifloEntity Raw<T>(T shape) where T : struct, IArchetypeQueryable
        => Store.GetEntityByRawEntity(EntityHandle.Of(shape).RawEntity);

    private T Spawn<T>(TestArea area) where T : struct, IArchetype => Entities.Create<T>(area);

    private TestArea Area(int id)
    {
        var area = Entities.Create<TestArea>();

        area.AreaId = id;

        return area;
    }

    [Fact]
    public void A_scoped_query_visits_only_what_the_scope_holds()
    {
        var north = Area(1);
        var south = Area(2);

        Spawn<Guard>(north).Route = 10;
        Spawn<Guard>(north).Route = 20;
        Spawn<Guard>(south).Route = 40;

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
        Entities.Create<Guard>().Route = 99;

        var routes = 0;

        foreach (var guard in Entities.Query<Guard>().InScope(north))
            routes += guard.Route;

        Assert.Equal(5, routes);
    }

    /// The scope holds it, but it is not the shape that was asked for.
    [Fact]
    public void A_shape_the_query_did_not_ask_for_is_not_visited()
    {
        var north = Area(1);

        Spawn<Guard>(north);
        Spawn<Critter>(north);

        var count = 0;

        foreach (var _ in Entities.Query<Guard>().InScope(north))
            count++;

        Assert.Equal(1, count);
    }

    /// A mixin narrows the same way, across every archetype carrying it.
    [Fact]
    public void A_mixin_query_can_be_scoped_too()
    {
        var north = Area(1);

        Spawn<Guard>(north).Hp = 3f;
        Spawn<Critter>(north).Hp = 4f;
        Spawn<Critter>(Area(2)).Hp = 5f;

        var total = 0f;

        foreach (var health in Entities.Query<Health>().InScope(north))
            total += health.Hp;

        Assert.Equal(7f, total);
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
    public void A_write_through_a_scoped_query_lands_on_the_entity()
    {
        var north = Area(1);
        var guard = Spawn<Guard>(north);

        foreach (var found in Entities.Query<Guard>().InScope(north))
            found.Route = 7;

        Assert.Equal(7, guard.Route);
    }

    /// <summary>
    /// The flow a mod writes: find the scope by the value it is indexed on, then narrow to it.
    /// </summary>
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

    [Fact]
    public void A_scoped_query_runs_a_body_over_what_it_matched()
    {
        var north = Area(1);

        Spawn<Guard>(north);
        Spawn<Guard>(north);

        var visited = 0;

        Entities.Query<Guard>().InScope(north).ForEach(_ => visited++);

        Assert.Equal(2, visited);
    }

    /// A scope is still a shape, so it answers to everything else a shape does.
    [Fact]
    public void A_scope_is_an_ordinary_archetype_as_well()
    {
        var north = Area(3);

        Assert.True(north.IsValid);
        Assert.Equal(3, north.AreaId);
        Assert.Equal(1, CountAreas());
    }

    private int CountAreas()
    {
        var count = 0;

        foreach (var _ in Entities.Query<TestArea>())
            count++;

        return count;
    }

    // -- creating inside a scope -------------------------------------------------------------------

    [Fact]
    public void An_entity_created_in_a_scope_is_held_by_it()
    {
        var north = Area(1);

        Entities.Create<Guard>(north);

        Assert.Equal(1, CountIn(north));
    }

    /// Creating without a scope leaves the entity global, so no scope holds it.
    [Fact]
    public void An_entity_created_without_a_scope_is_held_by_none()
    {
        var north = Area(1);

        Entities.Create<Guard>();

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

    /// The shape arrives whole, the same as an unscoped create.
    [Fact]
    public void An_entity_created_in_a_scope_still_carries_its_whole_shape()
    {
        var guard = Entities.Create<Guard>(Area(1));

        guard.Route = 4;
        guard.Hp = 9f;

        Assert.Equal(4, guard.Route);
        Assert.Equal(9f, guard.Hp);
    }

    private int CountIn(TestArea area)
    {
        var count = 0;

        foreach (var _ in Entities.Query<Guard>().InScope(area))
            count++;

        return count;
    }
}
