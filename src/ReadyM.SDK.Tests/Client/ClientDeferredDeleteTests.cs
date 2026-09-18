using ReadyM.SDK.Exceptions;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// Deleting from inside a query on the client, which has to behave as the server's does.
/// </summary>
/// <remarks>
/// The client holds entity ids for the length of a loop rather than chunk addresses, so a delete
/// mid-loop would leave the loop walking ids whose revision has moved on. Same rule, same shape: the
/// removal waits for the loop, the entity counts as gone straight away.
/// </remarks>
public class ClientDeferredDeleteTests : ClientSdkTest
{
    private void SpawnMonsters(int count)
    {
        for (var i = 0; i < count; i++)
            SpawnMonster(level: i);
    }

    [Fact]
    public void A_delete_outside_a_query_happens_at_once()
    {
        var monster = SpawnMonster();

        Assert.True(Entities.Delete(monster));
        Assert.False(monster.IsValid);
        Assert.Equal(0, Count(Entities.Query<Monster>()));
    }

    [Fact]
    public void A_delete_inside_a_query_lands_only_once_the_loop_ends()
    {
        SpawnMonsters(4);

        foreach (var monster in Entities.Query<Monster>())
        {
            Entities.Delete(monster);

            // Still here as far as the store is concerned: the loop is walking it.
            Assert.False(Store.GetEntityByRawEntity(monster.Handle.RawEntity).IsNull);
        }

        Assert.Equal(0, Count(Entities.Query<Monster>()));
    }

    [Fact]
    public void An_entity_deleted_earlier_in_the_body_throws_when_used_again()
    {
        SpawnMonsters(1);

        foreach (var monster in Entities.Query<Monster>())
        {
            var handle = monster.Handle;

            Entities.Delete(handle);

            Assert.False(handle.IsAlive());
            Assert.Throws<InvalidEntityException>(() => handle.GetComponent<HealthComponent>());
            Assert.Throws<InvalidEntityException>(() => handle.Is<Monster>());
        }
    }

    [Fact]
    public void An_archetype_of_a_deleted_entity_throws_when_read_again()
    {
        var spawned = SpawnMonster();

        foreach (var monster in Entities.Query<Monster>())
        {
            Entities.Delete(monster);

            Assert.False(spawned.IsValid);
            Assert.Throws<InvalidEntityException>(() => spawned.Hp);
        }
    }

    [Fact]
    public void Asking_twice_for_the_same_entity_deletes_it_once()
    {
        SpawnMonsters(1);

        foreach (var monster in Entities.Query<Monster>())
        {
            Assert.True(Entities.Delete(monster));
            Assert.False(Entities.Delete(monster));
        }

        Assert.Equal(0, Count(Entities.Query<Monster>()));
    }

    [Fact]
    public void A_nested_query_holds_the_delete_until_the_outer_one_ends()
    {
        SpawnMonsters(2);

        foreach (var monster in Entities.Query<Monster>())
        {
            foreach (var health in Entities.Query<Health>())
                Entities.Delete(health);

            Assert.False(Store.GetEntityByRawEntity(monster.Handle.RawEntity).IsNull);
        }

        Assert.Equal(0, Count(Entities.Query<Monster>()));
    }

    [Fact]
    public void What_a_query_deleted_is_gone_from_the_next_one()
    {
        SpawnMonsters(5);

        foreach (var monster in Entities.Query<Monster>())
            if (monster.Level % 2 == 0)
                Entities.Delete(monster);

        var levels = 0;

        foreach (var monster in Entities.Query<Monster>())
            levels += monster.Level;

        Assert.Equal(4, levels);
    }
}
