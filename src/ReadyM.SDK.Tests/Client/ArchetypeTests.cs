using ReadyM.SDK.Entities;
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
        // An archetype also carries a marker, and one for every archetype it includes, which is what
        // makes asking whether an entity is one mean identity rather than shape.
        Assert.Equal(
            ["MonsterComponent", "MonsterArchetypeMarker", "CreatureArchetypeMarker", "HealthComponent", "PlacementComponent"],
            ComponentsOf<Monster>().Types.Select(type => type.Name));

        Assert.Equal(
            ["ChestComponent", "ChestArchetypeMarker", "HealthComponent", "PlacementComponent"],
            ComponentsOf<Chest>().Types.Select(type => type.Name));

        // A mixin is found by the component it carries, so it needs no marker.
        Assert.Equal(["HealthComponent"], ComponentsOf<Health>().Types.Select(type => type.Name));
    }

    [Fact]
    public void Components_is_the_same_instance_every_time_it_is_read()
    {
        // The query path caches resolved component ids against this reference, so it has to be stable.
        Assert.Same(ComponentsOf<Monster>(), ComponentsOf<Monster>());
        Assert.Same(ComponentsOf<Health>(), ComponentsOf<Health>());
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
    public void A_chest_without_loot_is_not_a_looted_chest()
    {
        var chest = SpawnChest();

        Assert.False(chest.Is<LootedChest>());
        Assert.False(chest.TryAs<LootedChest>(out _));
        Assert.False(EntityHandle.Of(chest).TryAs<Loot>(out _));
    }

    [Fact]
    public void A_looted_chest_reads_and_writes_through_the_narrower_shape()
    {
        var looted = Entities.Create<LootedChest>();

        looted.Rarity = 4;

        Assert.Equal(4, looted.Rarity);
        Assert.True(EntityHandle.Of(looted).TryAs<Loot>(out var loot));

        loot.Rarity = 9;
        Assert.Equal(9, looted.Rarity);
    }

    /// The wider shape still reaches the narrower one, which is what the optional include was for.
    [Fact]
    public void A_query_for_the_wider_shape_reaches_both()
    {
        SpawnChest();
        Entities.Create<LootedChest>();

        Assert.Equal(2, Count(Entities.Query<Chest>()));
        Assert.Equal(1, Count(Entities.Query<LootedChest>()));
    }

    [Fact]
    public void A_looted_chest_converts_up_to_a_chest()
    {
        var looted = Entities.Create<LootedChest>();

        Chest chest = looted;

        Assert.Equal(EntityHandle.Of(looted).Id, EntityHandle.Of(chest).Id);
        Assert.True(chest.Is<LootedChest>());
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
