using ReadyM.SDK.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// The surface an archetype struct exposes on its own, outside a query.
/// </summary>
public class ArchetypeTests : ClientSdkTest
{
    [Fact]
    public void Components_names_the_required_components_of_the_archetype()
    {
        Assert.Equal(
            ["MonsterComponent", "HealthComponent", "PlacementComponent"],
            ComponentsOf<Monster>().Types.Select(type => type.Name));

        // An optional include is not part of what the archetype requires.
        Assert.Equal(
            ["ChestComponent", "HealthComponent", "PlacementComponent"],
            ComponentsOf<Chest>().Types.Select(type => type.Name));

        Assert.Equal(["HealthComponent"], ComponentsOf<Health>().Types.Select(type => type.Name));
    }

    [Fact]
    public void Components_is_the_same_instance_every_time_it_is_read()
    {
        // The query path caches resolved component ids against this reference, so it has to be stable.
        Assert.Same(ComponentsOf<Monster>(), ComponentsOf<Monster>());
        Assert.Same(ComponentsOf<Creature>(), ComponentsOf<Health>());
    }

    [Fact]
    public void Has_reports_whether_the_tag_is_on_the_entity()
    {
        var monster = SpawnMonster();
        Assert.False(monster.Has<Enraged>());

        AddTag<Enraged, Monster>(monster);
        Assert.True(monster.Has<Enraged>());
        Assert.False(monster.Has<Dormant>());
    }

    [Fact]
    public void Set_adds_and_removes_a_tag_outside_a_query()
    {
        var monster = SpawnMonster();

        monster.Set<Enraged>(true);
        Assert.True(monster.Has<Enraged>());

        monster.Set<Enraged>(false);
        Assert.False(monster.Has<Enraged>());
    }

    [Fact]
    public void Set_outside_a_query_is_visible_to_the_next_statement()
    {
        // Deferring only happens while a query iterates, so a tag toggled anywhere else lands at once
        // rather than waiting for some unrelated loop to end.
        var monster = SpawnMonster();

        monster.Set<Enraged>(true);

        Assert.True(monster.Has<Enraged>());
        Assert.Equal(1, Count(Entities.Query<Monster>().With<Enraged>()));
    }

    [Fact]
    public void An_accessor_declared_on_the_archetype_reads_and_writes_its_own_component()
    {
        var monster = SpawnMonster(level: 3);
        Assert.Equal(3, monster.Level);

        monster.Level = 7;
        Assert.Equal(7, monster.Level);
    }

    [Fact]
    public void An_archetype_converts_implicitly_to_one_it_includes()
    {
        var monster = SpawnMonster(hp: 12f, maxHp: 80f);

        Creature creature = monster;

        Assert.True(creature.IsValid);
        Assert.Equal(12f, creature.Hp);

        creature.Hp = 34f;
        Assert.Equal(34f, monster.Hp);
    }

    [Fact]
    public void The_converted_handle_points_at_the_same_entity()
    {
        var monster = SpawnMonster();
        Creature creature = monster;

        Assert.Equal(
            EntityHandle.Of(monster).Id,
            EntityHandle.Of(creature).Id);
    }

    [Fact]
    public void An_absent_optional_mixin_reads_as_null()
    {
        var chest = SpawnChest();

        Assert.Null(chest.Rarity);
        Assert.False(chest.TryGetLoot(out _));
        Assert.Throws<InvalidOperationException>(() => chest.RequireLoot());
    }

    [Fact]
    public void A_present_optional_mixin_reads_and_writes_through_the_narrower_handle()
    {
        var chest = SpawnChest(rarity: 4);

        Assert.Equal(4, chest.Rarity);

        Assert.True(chest.TryGetLoot(out var loot));
        loot.Rarity = 9;
        Assert.Equal(9, chest.Rarity);

        var required = chest.RequireLoot();
        required.Rarity = 11;
        Assert.Equal(11, chest.Rarity);
    }

    [Fact]
    public void An_optional_mixin_does_not_change_which_entities_a_query_matches()
    {
        SpawnChest(rarity: 1);
        SpawnChest();

        Assert.Equal(2, Count(Entities.Query<Chest>()));
    }

    [Fact]
    public void EnsureX_gives_a_usable_handle_outside_a_query()
    {
        var chest = SpawnChest();

        var loot = chest.EnsureLoot();
        loot.Rarity = 5;

        Assert.Equal(5, chest.Rarity);
    }

    [Fact]
    public void EnsureX_on_an_entity_that_already_has_the_mixin_keeps_its_values()
    {
        var chest = SpawnChest(rarity: 3);

        Assert.Equal(3, chest.EnsureLoot().Rarity);
    }

    [Fact]
    public void EnsureX_works_inside_a_query()
    {
        SpawnChest();

        foreach (var chest in Entities.Query<Chest>())
            chest.EnsureLoot().Rarity = 4;

        foreach (var chest in Entities.Query<Chest>())
            Assert.Equal(4, chest.Rarity);
    }

    /// <summary>
    /// Known gap. A default archetype struct has no API link, so asking it anything throws a
    /// NullReferenceException instead of reporting that it is invalid. TryGetX hands one out on
    /// failure, so it is reachable from ordinary code.
    /// </summary>
    [Fact]
    public void A_default_archetype_handle_throws_instead_of_reporting_invalid()
    {
        Assert.Throws<NullReferenceException>(() => default(Monster).IsValid);
    }
}
