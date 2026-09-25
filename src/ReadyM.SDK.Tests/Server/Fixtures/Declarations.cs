using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Tests.ExternalMod;

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

/// <summary>
/// Includes a mixin from an assembly compiled without the server SDK, so that mixin has no accessors
/// reachable from a chunk and this shape stays on the identity path.
/// </summary>
[Archetype]
[Include(typeof(Position))]
[Include(typeof(Wallet))]
public readonly partial struct Peddler
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

/// Carries nothing at all. Only its marker makes it findable, which is the point of having one.
[Archetype]
public readonly partial struct Landmark;

/// Everything a Landmark is, plus a position.
[Archetype]
[IncludeArchetype(typeof(Landmark))]
[Include(typeof(Position))]
public readonly partial struct PlacedLandmark;

/// Stands in for a component the relay already owns: PascalCase members, one of them readonly.
internal struct RelayMetadataComponent : IComponent
{
    public readonly int NetId;
    public int Owner;

    public RelayMetadataComponent(int netId, int owner)
    {
        NetId = netId;
        Owner = owner;
    }
}

[ArchetypeMixin]
[ExplicitComponent(typeof(RelayMetadataComponent))]
public readonly partial struct Metadata
{
    public partial int NetId { get; }
    public partial int Owner { get; set; }
}

/// An archetype over storage the relay owns, so it carries no marker.
[Archetype]
[Include(typeof(Position))]
[Include(typeof(Metadata))]
public readonly partial struct Networked;

/// Indexed by a property the SDK declares, so the component behind it is generated and mod-owned.
[ArchetypeMixin]
public readonly partial struct Ticketed
{
    [Index]
    public partial int Ticket { get; set; }

    public partial int Fare { get; set; }
}

[Archetype]
[Include(typeof(Position))]
[Include(typeof(Ticketed))]
public readonly partial struct Passenger;

/// Stands in for a component the relay owns and indexes itself, the way MetadataComponent is.
internal struct RelayNetIdComponent : IIndexedComponent<int>
{
    public int NetId;
    public int Owner;

    public int GetIndexedValue() => NetId;
}

[ArchetypeMixin]
[ExplicitComponent(typeof(RelayNetIdComponent))]
public readonly partial struct Networked2
{
    public partial int NetId { get; set; }
    public partial int Owner { get; set; }
}

[Archetype]
[Include(typeof(Position))]
[Include(typeof(Networked2))]
public readonly partial struct Replicated;

/// Stands in for a scope component the relay owns, indexed the way AreaScopeComponent is.
internal struct TestAreaScopeComponent : IIndexedComponent<int>
{
    public int AreaId;

    public int GetIndexedValue() => AreaId;
}

[Archetype]
[ExplicitComponent(typeof(TestAreaScopeComponent))]
public readonly partial struct TestArea : IScope
{
    public partial int AreaId { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Patrol
{
    public partial int Route { get; set; }
}

[Archetype]
[Include(typeof(Position))]
[Include(typeof(Patrol))]
public readonly partial struct Guard;
