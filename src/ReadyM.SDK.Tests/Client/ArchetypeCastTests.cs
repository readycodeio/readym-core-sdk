using ReadyM.SDK.Entities;
using ReadyM.SDK.Exceptions;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// Converting between archetypes: the checked forms C# cannot express as <c>is</c> or <c>as</c>.
/// </summary>
public class ArchetypeCastTests : ClientSdkTest
{
    [Fact]
    public void Is_holds_for_the_archetype_itself()
    {
        var monster = SpawnMonster();

        Assert.True(monster.Is<Monster>());
    }

    [Fact]
    public void Is_holds_for_an_included_archetype()
    {
        var monster = SpawnMonster();

        Assert.True(monster.Is<Creature>());
    }

    [Fact]
    public void Is_does_not_hold_for_an_unrelated_archetype()
    {
        var monster = SpawnMonster();

        Assert.False(monster.Is<Chest>());
    }

    /// <summary>
    /// Identity, not shape. Prop is Placement only, which Monster carries too, but nothing relates
    /// the two declarations and the generated marker is what says so.
    /// </summary>
    [Fact]
    public void Is_does_not_hold_for_an_archetype_that_merely_shares_components()
    {
        var monster = SpawnMonster();

        Assert.False(monster.Is<Prop>());
        Assert.True(monster.Is<Monster>());
    }

    [Fact]
    public void Is_on_a_deleted_entity_throws()
    {
        var monster = SpawnMonster();
        Store.GetEntityByRawEntity(EntityHandle.Of(monster).RawEntity).DeleteEntity();

        Assert.Throws<InvalidEntityException>(() => monster.Is<Creature>());
    }

    [Fact]
    public void TryAs_gives_a_handle_on_the_same_entity()
    {
        var monster = SpawnMonster(hp: 15f);

        Assert.True(monster.TryAs(out Creature creature));
        Assert.Equal(15f, creature.Hp);

        creature.Hp = 60f;
        Assert.Equal(60f, monster.Hp);
    }

    [Fact]
    public void TryAs_fails_and_yields_a_default_handle()
    {
        var monster = SpawnMonster();

        Assert.False(monster.TryAs(out Chest chest));

        // RawEntity refuses to box, so the out value is checked through its handle rather than Equals.
        Assert.Equal(0, EntityHandle.Of(chest).Id);
    }

    [Fact]
    public void An_explicit_cast_down_succeeds_when_the_entity_is_one()
    {
        var monster = SpawnMonster(level: 4);
        Creature creature = monster;

        var again = (Monster)creature;

        Assert.Equal(4, again.Level);
    }

    [Fact]
    public void An_explicit_cast_down_throws_when_the_entity_is_not_one()
    {
        var creature = Entities.Create<Creature>();

        Assert.Throws<InvalidCastException>(() => (Monster)creature);
    }

    [Fact]
    public void A_cast_survives_a_round_trip_through_the_included_archetype()
    {
        var monster = SpawnMonster(level: 2, hp: 5f);

        var roundTripped = (Monster)(Creature)monster;

        Assert.Equal(2, roundTripped.Level);
        Assert.Equal(5f, roundTripped.Hp);
        Assert.True(roundTripped.IsValid);
    }

    [Fact]
    public void Casts_work_on_entities_a_query_handed_out()
    {
        SpawnMonster(level: 1);
        Entities.Create<Creature>();

        var creatures = 0;
        var monsters = 0;

        foreach (var creature in Entities.Query<Creature>())
        {
            creatures++;
            if (creature.Handle.TryAs(out Monster monster))
            {
                monsters++;
                Assert.Equal(1, monster.Level);
            }
        }

        Assert.Equal(2, creatures);
        Assert.Equal(1, monsters);
    }
}