using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

public class QueryTests : ClientSdkTest
{
    [Fact]
    public void Query_visits_every_entity_of_the_archetype()
    {
        SpawnMonster(level: 1);
        SpawnMonster(level: 2);
        SpawnMonster(level: 3);
        SpawnChest();

        var levels = new List<int>();
        foreach (var monster in Entities.Query<Monster>())
            levels.Add(monster.Level);

        levels.Sort();
        Assert.Equal([1, 2, 3], levels);
    }

    [Fact]
    public void Query_over_an_empty_store_yields_nothing()
    {
        Assert.Equal(0, Count(Entities.Query<Monster>()));
    }

    [Fact]
    public void Accessors_from_an_included_mixin_read_the_entity_state()
    {
        SpawnMonster(hp: 30f, maxHp: 120f, x: 4f, y: 5f);

        foreach (var monster in Entities.Query<Monster>())
        {
            Assert.Equal(30f, monster.Hp);
            Assert.Equal(120f, monster.MaxHp);
            Assert.Equal(4f, monster.X);
            Assert.Equal(5f, monster.Y);
        }
    }

    [Fact]
    public void Writing_through_an_accessor_reaches_the_store()
    {
        var monster = SpawnMonster(hp: 10f, maxHp: 100f);

        foreach (var found in Entities.Query<Monster>())
            found.Hp = found.MaxHp;

        Assert.Equal(100f, monster.Hp);
    }

    [Fact]
    public void An_archetype_query_matches_the_entities_created_as_it()
    {
        // Prop is Placement only, which Monster and Chest carry too, but the marker keeps them out:
        // a query for an archetype is every entity of that archetype, and of ones including it.
        SpawnProp();
        SpawnMonster();
        SpawnChest();

        Assert.Equal(1, Count(Entities.Query<Prop>()));
        Assert.Equal(1, Count(Entities.Query<Monster>()));
        Assert.Equal(1, Count(Entities.Query<Chest>()));
    }

    [Fact]
    public void A_mixin_query_spans_archetypes()
    {
        SpawnMonster(hp: 1f);
        SpawnChest(hp: 2f);
        SpawnProp();

        var seen = new List<float>();
        foreach (var (health, placement) in Entities.Query<Health, Placement>())
        {
            seen.Add(health.Hp);
            Assert.Equal(0f, placement.X);
        }

        seen.Sort();
        Assert.Equal([1f, 2f], seen);
    }

    [Fact]
    public void A_mixin_query_writes_through_the_deconstructed_handle()
    {
        var monster = SpawnMonster(hp: 1f, x: 0f);

        foreach (var (health, placement) in Entities.Query<Health, Placement>())
        {
            health.Hp = 99f;
            placement.X = 7f;
        }

        Assert.Equal(99f, monster.Hp);
        Assert.Equal(7f, monster.X);
    }

    [Fact]
    public void With_keeps_only_tagged_entities()
    {
        var enraged = SpawnMonster(level: 1);
        SpawnMonster(level: 2);
        AddTag<Enraged, Monster>(enraged);

        var levels = new List<int>();
        foreach (var monster in Entities.Query<Monster>().With<Enraged>())
            levels.Add(monster.Level);

        Assert.Equal([1], levels);
    }

    [Fact]
    public void Without_drops_tagged_entities()
    {
        var dormant = SpawnMonster(level: 1);
        SpawnMonster(level: 2);
        AddTag<Dormant, Monster>(dormant);

        var levels = new List<int>();
        foreach (var monster in Entities.Query<Monster>().Without<Dormant>())
            levels.Add(monster.Level);

        Assert.Equal([2], levels);
    }

    [Fact]
    public void With_and_Without_chain()
    {
        var wanted = SpawnMonster(level: 1);
        var alsoDormant = SpawnMonster(level: 2);
        SpawnMonster(level: 3);

        AddTag<Enraged, Monster>(wanted);
        AddTag<Enraged, Monster>(alsoDormant);
        AddTag<Dormant, Monster>(alsoDormant);

        var levels = new List<int>();
        foreach (var monster in Entities.Query<Monster>().With<Enraged>().Without<Dormant>())
            levels.Add(monster.Level);

        Assert.Equal([1], levels);
    }

    [Fact]
    public void ForEach_visits_the_same_set_as_the_loop()
    {
        SpawnMonster(level: 1);
        SpawnMonster(level: 2);

        var levels = new List<int>();
        Entities.Query<Monster>().ForEach(monster => levels.Add(monster.Level));

        levels.Sort();
        Assert.Equal([1, 2], levels);
    }

    [Fact]
    public void Nested_read_only_queries_are_fine()
    {
        SpawnMonster(level: 1);
        SpawnMonster(level: 2);

        var pairs = 0;
        foreach (var outer in Entities.Query<Monster>())
        foreach (var inner in Entities.Query<Monster>())
        {
            Assert.True(outer.Level > 0);
            Assert.True(inner.Level > 0);
            pairs++;
        }

        Assert.Equal(4, pairs);
    }
}
