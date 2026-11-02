using Friflo.Engine.ECS;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// <summary>
/// Stands in for a networked component: the list is private and reached only through methods, so
/// every write stays tracked.
/// </summary>
internal struct SequencesComponent : IComponent
{
    private List<int> _started;
    private int _changes;

    public int StartedCount => _started?.Count ?? 0;

    public int Changes => _changes;

    public int GetStarted(int index) => _started[index];

    /// <summary>A ref struct over the component's own storage, which is what a native collection hands back.</summary>
    public ReadOnlySpan<int> GetStartedSpan() => _started is null ? default : _started.ToArray();

    public void AddStarted(in int value)
    {
        _started ??= [];
        _started.Add(value);
        _changes++;
    }

    public bool ContainsStarted(in int value) => _started is not null && _started.Contains(value);

    public void ClearStarted()
    {
        _started?.Clear();
        _changes++;
    }

    /// <summary>Replication plumbing, which a shape must never gain.</summary>
    public void Started_SetFromApi(List<int> value, int id) => _started = value;

    public void StartedNotifyChanged(int id) => _changes++;
}

[ArchetypeMixin(Replication.None)]
[Extends(typeof(CoreArea))]
[ExplicitComponent(typeof(SequencesComponent))]
[ExplicitCollection("Started")]
public readonly partial struct Sequences;

/// <summary>The archetype a core exposes, which mods extend without redeclaring.</summary>
[Archetype(Replication = Replication.None)]
[ExplicitComponent(typeof(CoreMetadataComponent))]
public readonly partial struct CoreArea
{
    public partial int Owner { get; set; }
}

/// <summary>A mod adding its own mixin to every area it creates.</summary>
[ArchetypeMixin(Replication.None)]
[Extends(typeof(CoreArea))]
public readonly partial struct Weather
{
    public partial int Storm { get; set; }
}

/// <summary>A second mod doing the same, which must not collide with the first.</summary>
[ArchetypeMixin(Replication.None)]
[Extends(typeof(CoreArea))]
public readonly partial struct Music
{
    public partial int Track { get; set; }
}

/// <summary>An archetype that includes a shape carrying a collection, to check flattening.</summary>
[Archetype(Networked = false)]
[Include(typeof(Sequences))]
public readonly partial struct Cutscene
{
    public partial int Chapter { get; set; }
}

/// <summary>A mod adding values that read as if CoreArea had always carried them.</summary>
[ArchetypeMixin(Replication.None)]
[Extends(typeof(CoreArea))]
public readonly partial struct Climate
{
    public partial int Temperature { get; set; }

    public partial int Rainfall { get; }
}

/// <summary>The same, under a prefix, so two mods can both add a Temperature.</summary>
[ArchetypeMixin(Replication.None)]
[Extends(typeof(CoreArea), "Ambient")]
public readonly partial struct Soundscape
{
    public partial int Temperature { get; set; }
}

/// <summary>A mixin whose values go over the wire, which is the whole of what a replicating Replication asks for.</summary>
[ArchetypeMixin(Replication.OwnershipBased)]
public readonly partial struct Telemetry
{
    public partial int Ticks { get; set; }

    public partial float Load { get; set; }
}

/// <summary>
/// An archetype carrying the replicated mixin, so an entity can exist to write through. Its own value
/// replicates with it, because a replicated archetype sends what it declares itself.
/// </summary>
[Archetype(Replication = Replication.OwnershipBased)]
[Include(typeof(Telemetry))]
[Include(typeof(Roster))]
public readonly partial struct Rig
{
    public partial int Serial { get; set; }
}

/// <summary>Replaced often enough that a lost change is corrected by the next one.</summary>
[ArchetypeMixin(Replication.OwnershipBased, Delivery = Delivery.Unreliable)]
public readonly partial struct Motion
{
    public partial float Speed { get; set; }
}

/// <summary>
/// A shape over storage that already replicates. It says nothing about replicating; the component
/// does, and the shape inherits it.
/// </summary>
[ArchetypeMixin(Replication.OwnershipBased)]
[Extends(typeof(CoreArea))]
[ExplicitComponent(typeof(global::ReadyM.Api.Multiplayer.ECS.Components.EmptyScopeDeletionComponent))]
public readonly partial struct Perishable;

/// <summary>
/// A replicated shape holding a native collection. Its component gets setter methods rather than a
/// property for that value, so the accessors have to write it the other way round.
/// </summary>
[ArchetypeMixin(Replication.OwnershipBased)]
public readonly partial struct Roster
{
    public partial int Size { get; set; }

    private partial global::Yooni.Native.Container.NativeList<int> Members { get; set; }
}

/// <summary>
/// An archetype taking the same mixin twice over is not possible, so the prefix earns its place on
/// a single include: the collection's members have to land under it too.
/// </summary>
[Archetype(Replication = Replication.OwnershipBased)]
[Include(typeof(Roster), "Crew")]
public readonly partial struct Barge
{
    public partial int Berth { get; set; }
}

/// <summary>
/// A mixin carrying a collection and extending an archetype under a prefix, which is the pair the
/// prefix has to reach: the values it declares and the members its collection forwards.
/// </summary>
[ArchetypeMixin(Replication.OwnershipBased)]
[Extends(typeof(CoreArea), "Guest")]
public readonly partial struct Ledgered
{
    public partial int Size { get; set; }

    private partial global::Yooni.Native.Container.NativeList<int> Names { get; set; }
}

/// <summary>
/// The shape of a mod's own thing built entirely from storage the game owns and already sends: no
/// component of its own, nothing of its own to replicate, and still an entity the other side has to
/// hear about. Oblivion's Furniture is this shape.
/// </summary>
[Archetype]
[Include(typeof(Perishable))]
public readonly partial struct Borrowed;

/// <summary>A replicating shape no game registers an archetype for, so only the SDK's own pass gives it one.</summary>
[Archetype]
[Include(typeof(Telemetry))]
public readonly partial struct Unbound;

/// <summary>Rig's replicated mixin on an entity that stays on this side, and needs no binding for it.</summary>
[Archetype(Networked = false)]
[Include(typeof(Telemetry))]
public readonly partial struct Gauge;

/// <summary>Replicated with nothing that sends, so the entity is the whole of what crosses.</summary>
[Archetype]
[Include(typeof(Placement))]
public readonly partial struct Signpost;
