using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[Archetype]
[IncludeArchetype(typeof(Creature))]
[Include(typeof(Health))]
[Include(typeof(Placement))]
[Include(typeof(Enraged), Optional: true)]
[Include(typeof(Dormant), Optional: true)]
public readonly partial struct Monster
{
    public partial int Level { get; set; }
}
