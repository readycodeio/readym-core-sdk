using ReadyM.SDK.Entity;
using ReadyM.SDK.Exceptions;
using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Deleting from inside a query, which has to wait until the loop is over.
/// </summary>
/// <remarks>
/// A query hands the loop chunk addresses the relay gave it once, so removing an entity mid-loop
/// would move data the loop is still walking. The delete is therefore held, and the entity counts as
/// gone the moment it is asked for, which is what stops a body reading a value about to disappear.
/// </remarks>
public class ServerDeferredDeleteTests : ServerSdkTest
{
    private void Spawn(int count)
    {
        for (var i = 0; i < count; i++)
            Spawn<Npc>().Hp = i;
    }

    [Fact]
    public void A_delete_outside_a_query_happens_at_once()
    {
        var npc = Spawn<Npc>();
        Relay.ResetCounters();

        Assert.True(Entities.Delete(npc));
        Assert.Equal(1, Relay.DeleteCalls);
    }

    /// <summary>The whole point: nothing reaches the relay until the loop is done with its chunks.</summary>
    [Fact]
    public void A_delete_inside_a_query_reaches_the_relay_only_once_the_loop_ends()
    {
        Spawn(4);
        Relay.ResetCounters();

        foreach (var npc in Entities.Query<Npc>())
        {
            Entities.Delete(npc);
            Assert.Equal(0, Relay.DeleteCalls);
        }

        Assert.Equal(4, Relay.DeleteCalls);
    }

    /// <summary>A held delete still ends the entity: the loop must not read it again.</summary>
    [Fact]
    public void An_entity_deleted_earlier_in_the_body_throws_when_used_again()
    {
        Spawn(1);

        foreach (var npc in Entities.Query<Npc>())
        {
            var handle = npc.Handle;

            Entities.Delete(npc);

            Assert.False(handle.IsAlive());
            Assert.Throws<InvalidEntityException>(() => handle.GetComponent<VitalsComponent>());
            Assert.Throws<InvalidEntityException>(() => handle.Is<Npc>());
        }
    }

    /// The same through an archetype, which is what a body written against the identity path holds.
    [Fact]
    public void An_archetype_of_a_deleted_entity_throws_when_read_again()
    {
        var npc = Spawn<Npc>();

        var entities = Entities.Query<Npc>().Identities();

        while (entities.MoveNext())
        {
            Entities.Delete(entities.Current);

            Assert.False(npc.IsValid);
            Assert.Throws<InvalidEntityException>(() => npc.Hp);
        }

        entities.Dispose();
    }

    [Fact]
    public void Asking_twice_for_the_same_entity_deletes_it_once()
    {
        Spawn(1);
        Relay.ResetCounters();

        foreach (var npc in Entities.Query<Npc>())
        {
            Assert.True(Entities.Delete(npc));
            Assert.False(Entities.Delete(npc));
        }

        Assert.Equal(1, Relay.DeleteCalls);
    }

    /// <summary>A query inside a query must not flush on the inner loop's chunks.</summary>
    [Fact]
    public void A_nested_query_holds_the_delete_until_the_outer_one_ends()
    {
        Spawn(2);
        Relay.ResetCounters();

        foreach (var npc in Entities.Query<Npc>())
        {
            foreach (var position in Entities.Query<Position>())
                Entities.Delete(position);

            Assert.Equal(0, Relay.DeleteCalls);
            _ = npc.Hp;
        }

        Assert.Equal(2, Relay.DeleteCalls);
    }

    [Fact]
    public void What_a_query_deleted_is_gone_from_the_next_one()
    {
        Spawn(5);

        foreach (var npc in Entities.Query<Npc>())
            if (npc.Hp % 2 == 0)
                Entities.Delete(npc);

        var remaining = 0;

        foreach (var npc in Entities.Query<Npc>())
            remaining += npc.Hp;

        Assert.Equal(4, remaining);
    }

    /// Deleting every entity a query visits leaves nothing behind.
    [Fact]
    public void A_query_may_delete_everything_it_visits()
    {
        Spawn(6);

        foreach (var npc in Entities.Query<Npc>())
            Entities.Delete(npc);

        Assert.Equal(0, CountIdentities(Entities.Query<Npc>()));
    }

    // -- one call shape, wherever the entity is named from ----------------------------------------

    /// <summary>
    /// The point of IEntityShape: a loop deletes the way code outside one does. An archetype, a
    /// mixin, the chunk view of either, and a bare handle all reach the same method.
    /// </summary>
    [Fact]
    public void Delete_takes_an_archetype_outside_a_query()
    {
        var npc = Spawn<Npc>();

        Assert.True(Entities.Delete(npc));
        Assert.False(npc.IsValid);
    }

    [Fact]
    public void Delete_takes_a_mixin_outside_a_query()
    {
        var npc = Spawn<Npc>();

        Assert.True(EntityHandle.Of(npc).TryAs<Position>(out var position));
        Assert.True(Entities.Delete(position));
        Assert.Equal(0, CountIdentities(Entities.Query<Npc>()));
    }

    [Fact]
    public void Delete_takes_an_archetype_view_inside_a_query()
    {
        Spawn<Npc>();
        Spawn<Npc>();

        foreach (var npc in Entities.Query<Npc>())
            Entities.Delete(npc);

        Assert.Equal(0, CountIdentities(Entities.Query<Npc>()));
    }

    /// A mixin's view spans every archetype carrying it, so this deletes across both.
    [Fact]
    public void Delete_takes_a_mixin_view_inside_a_query()
    {
        Spawn<Npc>();
        Spawn<Boulder>();

        foreach (var position in Entities.Query<Position>())
            Entities.Delete(position);

        Assert.Equal(0, CountIdentities(Entities.Query<Position>()));
    }

    /// One of a tuple still names the entity all of them are on.
    [Fact]
    public void Delete_takes_one_part_of_a_multi_shape_query()
    {
        Spawn<Npc>();

        foreach (var (position, _) in Entities.Query<Position, Vitals>())
            Entities.Delete(position);

        Assert.Equal(0, CountIdentities(Entities.Query<Npc>()));
    }

    // -- the identity path, at every width --------------------------------------------------------

    /// A multi-shape loop opens a scope the same way a single-shape one does.
    [Fact]
    public void A_delete_inside_a_multi_shape_identity_loop_is_held()
    {
        Spawn(2);
        Relay.ResetCounters();

        var entities = Entities.Query<Position, Vitals>().Identities();

        while (entities.MoveNext())
        {
            var (position, _) = entities.Current;

            Entities.Delete(position);

            Assert.Equal(0, Relay.DeleteCalls);
        }

        entities.Dispose();

        Assert.Equal(2, Relay.DeleteCalls);
    }

    [Fact]
    public void Creating_inside_a_multi_shape_identity_loop_is_refused()
    {
        Spawn(1);

        Assert.Throws<StructuralChangeInQueryException>(() =>
        {
            var entities = Entities.Query<Position, Vitals>().Identities();

            while (entities.MoveNext())
                Entities.Create<Npc>();

            entities.Dispose();
        });
    }

    /// The widest loop, so no arity is left without a scope.
    [Fact]
    public void A_delete_inside_a_six_shape_identity_loop_is_held()
    {
        var adventurer = Spawn<Adventurer>();
        Relay.ResetCounters();

        var entities = Entities.Query<Position, Vitals, Named, Speed, Wealth, Mood>().Identities();

        while (entities.MoveNext())
        {
            var (position, _, _, _, _, _) = entities.Current;

            Entities.Delete(position);

            Assert.Equal(0, Relay.DeleteCalls);
        }

        entities.Dispose();

        Assert.Equal(1, Relay.DeleteCalls);
        Assert.False(adventurer.IsValid);
    }
}
