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
