using Friflo.Engine.ECS;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// Stands in for a networked component: the list is private and reached only through methods, so
/// every write stays tracked.
internal struct SequencesComponent : IComponent
{
    private List<int> _started;
    private int _changes;

    public int StartedCount => _started?.Count ?? 0;

    public int Changes => _changes;

    public int GetStarted(int index) => _started[index];

    /// A ref struct over the component's own storage, which is what a native collection hands back.
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

    /// Replication plumbing, which a shape must never gain.
    public void Started_SetFromApi(List<int> value, int id) => _started = value;

    public void StartedNotifyChanged(int id) => _changes++;
}

[ArchetypeMixin]
[Extends(typeof(CoreArea))]
[ExplicitComponent(typeof(SequencesComponent))]
[ExplicitCollection("Started")]
public readonly partial struct Sequences;

/// The archetype a core exposes, which mods extend without redeclaring.
[Archetype]
[ExplicitComponent(typeof(CoreMetadataComponent))]
public readonly partial struct CoreArea
{
    public partial int Owner { get; set; }
}

/// A mod adding its own mixin to every area it creates.
[ArchetypeMixin]
[Extends(typeof(CoreArea))]
public readonly partial struct Weather
{
    public partial int Storm { get; set; }
}

/// A second mod doing the same, which must not collide with the first.
[ArchetypeMixin]
[Extends(typeof(CoreArea))]
public readonly partial struct Music
{
    public partial int Track { get; set; }
}

/// An archetype that includes a shape carrying a collection, to check flattening.
[Archetype]
[Include(typeof(Sequences))]
public readonly partial struct Cutscene
{
    public partial int Chapter { get; set; }
}

/// A mod adding values that read as if CoreArea had always carried them.
[ArchetypeMixin]
[Extends(typeof(CoreArea))]
public readonly partial struct Climate
{
    public partial int Temperature { get; set; }

    public partial int Rainfall { get; }
}

/// The same, under a prefix, so two mods can both add a Temperature.
[ArchetypeMixin]
[Extends(typeof(CoreArea), "Ambient")]
public readonly partial struct Soundscape
{
    public partial int Temperature { get; set; }
}

/// A mixin whose values go over the wire, which is the whole of what [Replicated] asks for.
[ArchetypeMixin]
[Replicated]
public readonly partial struct Telemetry
{
    public partial int Ticks { get; set; }

    public partial float Load { get; set; }
}

/// An archetype carrying the replicated mixin, so an entity can exist to write through.
[Archetype]
[Include(typeof(Telemetry))]
[Include(typeof(Roster))]
public readonly partial struct Rig
{
    public partial int Serial { get; set; }
}

/// Replaced often enough that a lost change is corrected by the next one.
[ArchetypeMixin]
[Replicated(Delivery.Unreliable)]
public readonly partial struct Motion
{
    public partial float Speed { get; set; }
}

/// A shape over storage that already replicates. It says nothing about replicating; the component
/// does, and the shape inherits it.
[ArchetypeMixin]
[ExplicitComponent(typeof(global::ReadyM.Api.Multiplayer.ECS.Components.EmptyScopeDeletionComponent))]
public readonly partial struct Perishable;

/// A replicated shape holding a native collection. Its component gets setter methods rather than a
/// property for that value, so the accessors have to write it the other way round.
[ArchetypeMixin]
[Replicated]
public readonly partial struct Roster
{
    public partial int Size { get; set; }

    private partial global::Yooni.Native.Container.NativeList<int> Members { get; set; }
}
