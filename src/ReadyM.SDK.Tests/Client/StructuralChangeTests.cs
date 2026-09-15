using System.Threading;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// Toggling a tag inside a query loop, which the client defers through a command buffer played back
/// when the loop ends.
/// </summary>
public class StructuralChangeTests : ClientSdkTest
{
    [Fact]
    public void A_tag_set_inside_a_loop_is_applied_when_the_loop_ends()
    {
        SpawnMonster();

        foreach (var monster in Entities.Query<Monster>())
        {
            monster.Set<Enraged>(true);

            // Deferred, so the loop body does not see its own change. shape-by-example.md
            // (2026-09-08-query-service) asks for this to be immediate.
            Assert.False(monster.Has<Enraged>());
        }

        Assert.Equal(1, Count(Entities.Query<Monster>().With<Enraged>()));
    }

    [Fact]
    public void A_tag_cleared_inside_a_loop_is_applied_when_the_loop_ends()
    {
        var monster = SpawnMonster();
        AddTag<Enraged, Monster>(monster);

        foreach (var found in Entities.Query<Monster>().With<Enraged>())
            found.Set<Enraged>(false);

        Assert.Equal(0, Count(Entities.Query<Monster>().With<Enraged>()));
        Assert.Equal(1, Count(Entities.Query<Monster>()));
    }

    [Fact]
    public void An_entity_that_stops_matching_is_still_visited_by_the_running_loop()
    {
        AddTag<Enraged, Monster>(SpawnMonster(level: 1));
        AddTag<Enraged, Monster>(SpawnMonster(level: 2));

        var visited = 0;
        foreach (var monster in Entities.Query<Monster>().With<Enraged>())
        {
            monster.Set<Enraged>(false);
            visited++;
        }

        Assert.Equal(2, visited);
        Assert.Equal(0, Count(Entities.Query<Monster>().With<Enraged>()));
    }

    [Fact]
    public void ForEach_plays_back_when_it_returns()
    {
        SpawnMonster();

        Entities.Query<Monster>().ForEach(monster => monster.Set<Enraged>(true));

        Assert.Equal(1, Count(Entities.Query<Monster>().With<Enraged>()));
    }

    [Fact]
    public void Accessor_writes_are_not_deferred()
    {
        var monster = SpawnMonster(hp: 1f);

        foreach (var found in Entities.Query<Monster>())
        {
            found.Hp = 42f;
            Assert.Equal(42f, found.Hp);
        }

        Assert.Equal(42f, monster.Hp);
    }

    [Fact]
    public void A_deleted_entity_is_no_longer_valid()
    {
        var monster = SpawnMonster();
        Assert.True(monster.IsValid);

        Store.GetEntityByRawEntity(EntityHandle.Of(monster).RawEntity).DeleteEntity();

        Assert.False(monster.IsValid);
    }

    [Fact]
    public void A_handle_to_a_deleted_entity_stays_invalid_when_its_id_is_reused()
    {
        var monster = SpawnMonster(level: 1);
        var id = EntityHandle.Of(monster).Id;
        Store.GetEntityByRawEntity(EntityHandle.Of(monster).RawEntity).DeleteEntity();

        var replacement = SpawnMonster(level: 2);

        Assert.Equal(id, EntityHandle.Of(replacement).Id);
        Assert.False(monster.IsValid);
        Assert.True(replacement.IsValid);
        Assert.Equal(2, replacement.Level);
    }

    /// <summary>
    /// Known limitation. The command buffer lives on the entity API rather than on the enumerator, so
    /// an inner loop plays back the outer loop's pending changes, and it does so while the outer
    /// enumerator still holds the store's read lock.
    /// </summary>
    [Fact]
    public void A_nested_query_throws_when_the_outer_loop_has_queued_a_change()
    {
        SpawnMonster();

        Assert.Throws<LockRecursionException>(() =>
        {
            foreach (var outer in Entities.Query<Monster>())
            {
                outer.Set<Enraged>(true);

                foreach (var inner in Entities.Query<Monster>())
                    _ = inner.Level;
            }
        });
    }
}
