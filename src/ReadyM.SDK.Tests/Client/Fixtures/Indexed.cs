using Friflo.Engine.ECS;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// Indexed by a property the SDK declares, so the component behind it is generated.
[ArchetypeMixin]
public readonly partial struct Registered
{
    [Index]
    public partial int Ticket { get; set; }

    public partial string Note { get; set; }
}

/// Stands in for a component a core already owns, already indexed.
internal struct CoreTagComponent : IIndexedComponent<string>
{
    public string Tag;
    public int Weight;

    public string GetIndexedValue() => Tag;
}

/// Indexed because the component it borrows already is.
[ArchetypeMixin]
[ExplicitComponent(typeof(CoreTagComponent))]
public readonly partial struct Tagged
{
    public partial string Tag { get; set; }
    public partial int Weight { get; set; }
}

[Archetype]
[Include(typeof(Registered))]
[Include(typeof(Tagged))]
public readonly partial struct Ticketed;

/// An archetype indexed by its own property.
[Archetype]
public readonly partial struct Station
{
    [Index]
    public partial int Code { get; set; }
}

/// Replicated as well as indexed, so its values are reachable through tokens. That is the pair the
/// token has to write back whole, the way an indexed setter does.
[ArchetypeMixin]
[Replicated]
[Propagates(Propagation.OwnershipBased)]
public readonly partial struct Berth
{
    [Index]
    public partial int Slot { get; set; }

    public partial int Deck { get; set; }
}

[Archetype]
[Include(typeof(Berth))]
public readonly partial struct Docked;
