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
        Api.Playback();
        Assert.True(monster.Has<Enraged>());

        monster.Set<Enraged>(false);
        Api.Playback();
        Assert.False(monster.Has<Enraged>());
    }

    [Fact]
    public void Set_outside_a_query_does_nothing_until_something_plays_the_buffer_back()
    {
        // Nothing but a query enumerator calls Playback today, so a tag toggled outside a loop sits
        // in the command buffer until the next query happens to end.
        var monster = SpawnMonster();

        monster.Set<Enraged>(true);
        Assert.False(monster.Has<Enraged>());

        foreach (var _ in Entities.Query<Monster>()) { }

        Assert.True(monster.Has<Enraged>());
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

    /// <summary>
    /// Known limitation. EnsureX adds a component, which the client defers to the command buffer, so
    /// the handle it returns points at a component that does not exist yet.
    /// </summary>
    [Fact]
    public void EnsureX_does_not_produce_a_usable_handle_yet()
    {
        var chest = SpawnChest();

        var loot = chest.EnsureLoot();
        Assert.Throws<NullReferenceException>(() => loot.Rarity = 1);

        // It lands once something plays the buffer back.
        foreach (var _ in Entities.Query<Chest>()) { }

        Assert.Equal(0, chest.Rarity);
        chest.RequireLoot().Rarity = 5;
        Assert.Equal(5, chest.Rarity);
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
