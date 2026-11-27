using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Tests.ExternalMod;

namespace ReadyM.SDK.Tests.Server.Fixtures;

[ArchetypeMixin(Replication.None)]
public readonly partial struct Position
{
    public partial float X { get; set; }

    public partial float Y { get; set; }
}

[ArchetypeMixin(Replication.None)]
public readonly partial struct Vitals
{
    public partial int Hp { get; set; }

    public partial int MaxHp { get; set; }
}

/// <summary>Its value is a managed reference, so it can never live in a pinned heap.</summary>
[ArchetypeMixin(Replication.None)]
public readonly partial struct Named
{
    public partial string Label { get; set; }
}

[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Vitals))]
[Include(typeof(Named))]
public readonly partial struct Npc
{
    public partial int Faction { get; set; }
}

[Archetype(Networked = false)]
[Include(typeof(Position))]
public readonly partial struct Boulder
{
    public partial float Mass { get; set; }
}

/// <summary>An archetype that is nothing but a position, so it is a strict subset of the other two.</summary>
[Archetype(Networked = false)]
[Include(typeof(Position))]
public readonly partial struct Placed;

/// <summary>
/// Includes a mixin from an assembly compiled without the server SDK, so that mixin has no accessors
/// reachable from a chunk and this shape stays on the identity path.
/// </summary>
[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Wallet))]
public readonly partial struct Peddler
{
    public partial int Steps { get; set; }
}

// Three more mixins, so a query can reach the six the composed views go up to.

[ArchetypeMixin(Replication.None)]
public readonly partial struct Speed
{
    public partial float Pace { get; set; }
}

[ArchetypeMixin(Replication.None)]
public readonly partial struct Wealth
{
    public partial int Gold { get; set; }
}

[ArchetypeMixin(Replication.None)]
public readonly partial struct Mood
{
    public partial float Spirit { get; set; }
}

/// <summary>Carries all six, and nothing of its own, so a query over any subset of them matches it.</summary>
[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Vitals))]
[Include(typeof(Named))]
[Include(typeof(Speed))]
[Include(typeof(Wealth))]
[Include(typeof(Mood))]
public readonly partial struct Adventurer;

// Composition instead of an optional include: the narrower shape includes the wider one.

[ArchetypeMixin(Replication.None)]
public readonly partial struct QuestGiver
{
    public partial int QuestId { get; set; }
}

/// <summary>
/// Everything an Npc is, plus a quest. A query for Npc reaches these too; one for QuestNpc does not
/// reach a plain Npc.
/// </summary>
[Archetype(Networked = false)]
[IncludeArchetype(typeof(Npc))]
[Include(typeof(QuestGiver))]
public readonly partial struct QuestNpc;

/// <summary>Carries nothing at all. Only its marker makes it findable, which is the point of having one.</summary>
[Archetype(Networked = false)]
public readonly partial struct Landmark;

/// <summary>Everything a Landmark is, plus a position.</summary>
[Archetype(Networked = false)]
[IncludeArchetype(typeof(Landmark))]
[Include(typeof(Position))]
public readonly partial struct PlacedLandmark;

/// <summary>Stands in for a component the relay already owns: PascalCase members, one of them readonly.</summary>
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

[ArchetypeMixin(Replication.None)]
[ExplicitComponent(typeof(RelayMetadataComponent))]
public readonly partial struct Metadata
{
    public partial int NetId { get; }
    public partial int Owner { get; set; }
}

/// <summary>An archetype over storage the relay owns, so it carries no marker.</summary>
[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Metadata))]
public readonly partial struct Networked;

/// <summary>Indexed by a property the SDK declares, so the component behind it is generated and mod-owned.</summary>
[ArchetypeMixin(Replication.None)]
public readonly partial struct Ticketed
{
    [Index]
    public partial int Ticket { get; set; }

    public partial int Fare { get; set; }
}

[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Ticketed))]
public readonly partial struct Passenger;

/// <summary>Stands in for a component the relay owns and indexes itself, the way MetadataComponent is.</summary>
internal struct RelayNetIdComponent : IIndexedComponent<int>
{
    public int NetId;
    public int Owner;

    public int GetIndexedValue() => NetId;
}

[ArchetypeMixin(Replication.None)]
[ExplicitComponent(typeof(RelayNetIdComponent))]
public readonly partial struct Networked2
{
    public partial int NetId { get; set; }
    public partial int Owner { get; set; }
}

[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Networked2))]
public readonly partial struct Replicated;

/// <summary>Stands in for a scope component the relay owns, indexed the way AreaScopeComponent is.</summary>
internal struct TestAreaScopeComponent : IIndexedComponent<int>
{
    public int AreaId;

    public int GetIndexedValue() => AreaId;
}

[Archetype(Networked = false)]
[ExplicitComponent(typeof(TestAreaScopeComponent))]
public readonly partial struct TestArea : IScope
{
    public partial int AreaId { get; set; }
}

/// <summary>Stands in for a tag the relay's own systems ask about, such as whether an owner may change.</summary>
public readonly struct TestWatchedTag : Friflo.Engine.ECS.ITag;

public readonly struct TestSecondTag : Friflo.Engine.ECS.ITag;

/// <summary>A shape whose entities carry tags from the moment they exist.</summary>
[Archetype(Networked = false)]
[Tag(typeof(TestWatchedTag))]
[Tag(typeof(TestSecondTag))]
public readonly partial struct Watched
{
    public partial int Marker { get; set; }
}

/// <summary>A scope that sits inside another, which is what a cell is to an area.</summary>
[Archetype(Networked = false)]
public readonly partial struct TestRoom : IScope
{
    public partial int RoomId { get; set; }
}

[ArchetypeMixin(Replication.None)]
public readonly partial struct Patrol
{
    public partial int Route { get; set; }
}

[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Patrol))]
public readonly partial struct Guard;

/// <summary>A mixin whose values go over the wire, which is what makes an entity of it networked.</summary>
[ArchetypeMixin(Replication.OwnershipBased)]
public readonly partial struct Telemetry
{
    public partial int Ticks { get; set; }
}

/// <summary>A shape the server creates over the network.</summary>
[Archetype]
[Include(typeof(Position))]
[Include(typeof(Telemetry))]
public readonly partial struct Beacon;

/// <summary>The same, for the case of one no game registered an archetype for.</summary>
[Archetype]
[Include(typeof(Telemetry))]
public readonly partial struct Unbound;

/// <summary>
/// Beacon's components on an entity that never leaves the server, which is what Oblivion's Spawner is.
/// </summary>
[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Telemetry))]
public readonly partial struct Probe;

/// <summary>Replicated with nothing that sends, so the entity is the whole of what crosses.</summary>
[Archetype]
[Include(typeof(Position))]
public readonly partial struct Waypoint;

/// <summary>Stands in for a component the relay indexes but a game registered without its key.</summary>
internal struct UnkeyedComponent : IIndexedComponent<int>
{
    public int Key;

    public int GetIndexedValue() => Key;
}

[ArchetypeMixin(Replication.None)]
[ExplicitComponent(typeof(UnkeyedComponent))]
public readonly partial struct Unkeyed
{
    public partial int Key { get; set; }
}

[Archetype(Networked = false)]
[Include(typeof(Position))]
[Include(typeof(Unkeyed))]
public readonly partial struct Misregistered;
