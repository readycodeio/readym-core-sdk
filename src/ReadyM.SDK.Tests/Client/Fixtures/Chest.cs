using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[Archetype]
[Include(typeof(Health))]
[Include(typeof(Placement))]
[Include(typeof(Loot), Optional: true)]
public readonly partial struct Chest
{
    public partial int Gold { get; set; }
}
