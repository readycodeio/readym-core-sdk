using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Server.Fixtures;

[ArchetypeMixin]
public readonly partial struct Position
{
    public partial float X { get; set; }

    public partial float Y { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Vitals
{
    public partial int Hp { get; set; }

    public partial int MaxHp { get; set; }
}

/// Its value is a managed reference, so it can never live in a pinned heap.
[ArchetypeMixin]
public readonly partial struct Named
{
    public partial string Label { get; set; }
}

[Archetype]
[Include(typeof(Position))]
[Include(typeof(Vitals))]
[Include(typeof(Named))]
public readonly partial struct Npc
{
    public partial int Faction { get; set; }
}

[Archetype]
[Include(typeof(Position))]
public readonly partial struct Boulder
{
    public partial float Mass { get; set; }
}

/// An archetype that is nothing but a position, so it is a strict subset of the other two.
[Archetype]
[Include(typeof(Position))]
public readonly partial struct Placed;

/// An optional include means a matching chunk may not carry it, so this shape gets no chunk view.
[Archetype]
[Include(typeof(Position))]
[Include(typeof(Vitals), Optional: true)]
public readonly partial struct Wanderer
{
    public partial int Steps { get; set; }
}

// Three more mixins, so a query can reach the six the composed views go up to.

[ArchetypeMixin]
public readonly partial struct Speed
{
    public partial float Pace { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Wealth
{
    public partial int Gold { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Mood
{
    public partial float Spirit { get; set; }
}

/// Carries all six, and nothing of its own, so a query over any subset of them matches it.
[Archetype]
[Include(typeof(Position))]
[Include(typeof(Vitals))]
[Include(typeof(Named))]
[Include(typeof(Speed))]
[Include(typeof(Wealth))]
[Include(typeof(Mood))]
public readonly partial struct Adventurer;

// Composition instead of an optional include: the narrower shape includes the wider one.

[ArchetypeMixin]
public readonly partial struct QuestGiver
{
    public partial int QuestId { get; set; }
}

/// Everything an Npc is, plus a quest. A query for Npc reaches these too; one for QuestNpc does not
/// reach a plain Npc.
[Archetype]
[IncludeArchetype(typeof(Npc))]
[Include(typeof(QuestGiver))]
public readonly partial struct QuestNpc;
