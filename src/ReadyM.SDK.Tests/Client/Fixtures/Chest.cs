using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[Archetype]
[Include(typeof(Health))]
[Include(typeof(Placement))]
public readonly partial struct Chest
{
    public partial int Gold { get; set; }
}

/// Everything a chest is, plus loot. Composition in place of an optional include.
[Archetype]
[IncludeArchetype(typeof(Chest))]
[Include(typeof(Loot))]
public readonly partial struct LootedChest;
