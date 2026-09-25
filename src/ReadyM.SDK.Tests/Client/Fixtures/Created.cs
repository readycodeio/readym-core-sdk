using Microsoft.Extensions.Logging;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// What a shape does for itself as one of its entities appears: the place for a default.
[ArchetypeMixin]
public readonly partial struct Stamped
{
    public partial int Mark { get; set; }

    [CreateHandler]
    private void OnCreated() => Mark = 7;
}

/// The same, asking the game for what it needs. A handler's parameters come from its services.
[ArchetypeMixin]
public readonly partial struct Announced
{
    public partial int Heard { get; set; }

    [CreateHandler]
    private void OnCreated(ILogger log)
    {
        log.LogDebug("announced");
        Heard = 1;
    }
}

[Archetype]
[Include(typeof(Stamped))]
public readonly partial struct Parcel;

/// An archetype's own handler runs as well as the one on what it includes. Which of them goes first
/// is nobody's to say, so this sets a value of its own rather than reading the other's.
[Archetype]
[Include(typeof(Stamped))]
public readonly partial struct Crate
{
    public partial int Seal { get; set; }

    [CreateHandler]
    private void OnCreated() => Seal = 1;
}

[Archetype]
[Include(typeof(Announced))]
public readonly partial struct Herald;

/// Several mixins each asking for something, under an archetype asking for something of its own.
/// Creating one entity runs all of them, which is what makes a default a mixin's own business.
[Archetype]
[Include(typeof(Stamped))]
[Include(typeof(Announced))]
[Include(typeof(Counted))]
public readonly partial struct Convoy
{
    public partial int Sealed { get; set; }

    [CreateHandler]
    private void OnCreated() => Sealed = 4;
}

[ArchetypeMixin]
public readonly partial struct Counted
{
    public partial int Tally { get; set; }

    [CreateHandler]
    private void OnCreated() => Tally = 3;
}
