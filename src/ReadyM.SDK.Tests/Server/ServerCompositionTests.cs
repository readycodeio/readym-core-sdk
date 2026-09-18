using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Composition in place of an optional include: a narrower archetype includes a wider one, and a
/// query for the wider shape reaches both.
/// </summary>
public class ServerCompositionTests : ServerSdkTest
{
    [Fact]
    public void A_query_for_the_wider_shape_reaches_the_narrower_one()
    {
        Spawn<Npc>();
        Spawn<QuestNpc>();

        Assert.Equal(2, CountIdentities(Entities.Query<Npc>()));
        Assert.Equal(1, CountIdentities(Entities.Query<QuestNpc>()));
    }

    [Fact]
    public void The_narrower_shape_carries_everything_the_wider_one_does()
    {
        var quest = Spawn<QuestNpc>();

        quest.Faction = 1;
        quest.X = 2f;
        quest.Hp = 3;
        quest.Label = "four";
        quest.QuestId = 5;

        Assert.Equal(1, quest.Faction);
        Assert.Equal(2f, quest.X);
        Assert.Equal(3, quest.Hp);
        Assert.Equal("four", quest.Label);
        Assert.Equal(5, quest.QuestId);
    }

    /// <summary>
    /// An archetype that includes another still walks by chunk: the included shape's components are
    /// flattened into the slot order at compile time.
    /// </summary>
    [Fact]
    public void A_composed_archetype_crosses_the_boundary_once()
    {
        for (var i = 0; i < 10; i++)
        {
            var quest = Spawn<QuestNpc>();

            quest.Faction = 1;
            quest.X = 2f;
            quest.Hp = 3;
            quest.Label = "four";
            quest.QuestId = 5;
        }

        Relay.ResetCounters();

        var visited = 0;

        foreach (var quest in Entities.Query<QuestNpc>())
        {
            Assert.Equal(1, quest.Faction);
            Assert.Equal(2f, quest.X);
            Assert.Equal(3, quest.Hp);
            Assert.Equal("four", quest.Label);
            Assert.Equal(5, quest.QuestId);
            visited++;
        }

        Assert.Equal(10, visited);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    /// The wider query reaches the narrower entities by chunk as well.
    [Fact]
    public void The_wider_query_reaches_both_archetypes_by_chunk()
    {
        Spawn<Npc>().Faction = 1;
        Spawn<QuestNpc>().Faction = 2;
        Relay.ResetCounters();

        var factions = 0;

        foreach (var npc in Entities.Query<Npc>())
            factions += npc.Faction;

        Assert.Equal(3, factions);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    /// <summary>
    /// What replaces TryGetX on an optional include: ask the entity whether it carries the mixin.
    /// </summary>
    [Fact]
    public void An_entity_can_be_asked_whether_it_carries_a_mixin()
    {
        var quest = Spawn<QuestNpc>();
        var plain = Spawn<Npc>();

        Assert.True(quest.Is<QuestGiver>());
        Assert.False(plain.Is<QuestGiver>());

        Assert.True(EntityHandle.Of(quest).TryAs<QuestGiver>(out var giver));
        Assert.False(EntityHandle.Of(plain).TryAs<QuestGiver>(out _));

        giver.QuestId = 7;
        Assert.Equal(7, quest.QuestId);
    }

    /// The generated conversions carry an entity between the two shapes.
    [Fact]
    public void A_narrower_entity_converts_up_and_a_wider_one_reports_what_it_is()
    {
        var quest = Spawn<QuestNpc>();
        var plain = Spawn<Npc>();

        Npc widened = quest;

        Assert.True(widened.IsValid);
        Assert.True(widened.Is<QuestNpc>());
        Assert.False(plain.Is<QuestNpc>());
    }

    /// <summary>
    /// An archetype with no components of its own is still a shape, and its marker is what makes it
    /// one. Before markers it had an empty component set and could not be queried at all.
    /// </summary>
    [Fact]
    public void An_archetype_with_no_components_is_queryable()
    {
        Entities.Create<Landmark>();
        Entities.Create<Landmark>();
        Entities.Create<PlacedLandmark>();
        Entities.Create<Npc>();

        Relay.ResetCounters();

        var landmarks = 0;

        foreach (var _ in Entities.Query<Landmark>())
            landmarks++;

        Assert.Equal(3, landmarks);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    [Fact]
    public void The_narrower_landmark_is_reached_only_by_its_own_query()
    {
        Entities.Create<Landmark>();

        var placed = Entities.Create<PlacedLandmark>();

        placed.X = 9f;

        var count = 0;

        foreach (var landmark in Entities.Query<PlacedLandmark>())
        {
            Assert.Equal(9f, landmark.X);
            count++;
        }

        Assert.Equal(1, count);
    }
}
