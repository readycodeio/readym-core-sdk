using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[Archetype]
[Include(typeof(Vitals))]
[Include(typeof(Hostile))]
public readonly partial struct Npc;
